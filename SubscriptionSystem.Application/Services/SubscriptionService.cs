using Microsoft.Extensions.Logging;
using SubscriptionSystem.Application.Interfaces;
using SubscriptionSystem.Application.DTOs;
using SubscriptionSystem.Domain.Entities;
using SubscriptionSystem.Domain.Enums;
using SubscriptionSystem.Application.Common;
using System.Net.Mail;
using Microsoft.Extensions.Caching.Memory;
using SubscriptionSystem.Domain.Events;

namespace SubscriptionSystem.Application.Services
{
    public class SubscriptionService : ISubscriptionService
    {
        private readonly ISubscriptionRepository _subscriptionRepository;
        private readonly IEmailService _emailService;
        private readonly IUserRepository _userRepository;
        private readonly IPaymentService _paymentService;
        private readonly ILogger<SubscriptionService> _logger;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IMemoryCache _cache;
    private readonly IDomainEventPublisher _eventPublisher;

        public SubscriptionService(
            ISubscriptionRepository subscriptionRepository,
            IEmailService emailService,
            IUserRepository userRepository,
            IPaymentService paymentService,
            IPaymentRepository paymentRepository,
            IDomainEventPublisher eventPublisher,
            IMemoryCache cache,
            ILogger<SubscriptionService> logger)
        {
            _subscriptionRepository = subscriptionRepository ?? throw new ArgumentNullException(nameof(subscriptionRepository));
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _paymentService = paymentService ?? throw new ArgumentNullException(nameof(paymentService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _paymentRepository = paymentRepository;
            _eventPublisher = eventPublisher;
            _cache = cache;
        }

        public async Task HandleICellDataSyncAsync(string msisdn, string? productId, string? errorCode, string? errorMsg)
        {
            try
            {
                _logger.LogInformation("ICell DataSync: msisdn={Msisdn}, productId={ProductId}, code={Code}, msg={Msg}", msisdn, productId, errorCode, errorMsg);

                if (string.Equals(errorCode, "1000", StringComparison.OrdinalIgnoreCase))
                {
                    // Normalize MSISDN if needed (assume provider gives full international code already)
                    var user = await _user_repository_get_by_phone(msisdn);

                    if (user == null)
                    {
                        // Create a new user with temporary credentials
                        var tempPassword = GenerateTemporaryPassword();

                        var newUser = new User
                        {
                            Id = Guid.NewGuid().ToString(),
                            PhoneNumber = msisdn,
                            Email = null, // no email provided
                            FullName = $"Subscriber {msisdn}",
                            PasswordHash = HashPassword(tempPassword),
                            TemporaryPassword = tempPassword, // stored clearly until user changes it
                            MustChangePassword = true,
                            Role = "User",
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        };

                        await _userRepository.CreateUserAsync(newUser);
                        user = newUser;

                        _logger.LogInformation("Created new user for msisdn={Msisdn} with temporary password", msisdn);
                    }

                    if (user != null)
                    {
                        var active = await _subscriptionRepository.GetActiveSubscriptionAsync(user.Id);
                        if (active == null)
                        {
                            var subscription = new Subscription
                            {
                                Id = Guid.NewGuid(),
                                UserId = user.Id,
                                Plan = SubscriptionPlan.OneDay,
                                PlanType = SubscriptionPlan.OneDay.ToString(),
                                AmountPaid = 0,
                                Currency = "NGN",
                                StartDate = DateTime.UtcNow,
                                ExpiryDate = DateTime.UtcNow.AddDays(1),
                                IsActive = true,
                                Status = SubscriptionStatuses.Active,
                                TransactionId = Guid.NewGuid().ToString()
                            };
                            await _subscriptionRepository.AddAsync(subscription);
                            await _eventPublisher.PublishAsync(new SubscriptionActivatedEvent(user.Id, subscription.Id, subscription.Plan, subscription.ExpiryDate, subscription.AmountPaid));
                        }
                        else
                        {
                            active.ExpiryDate = active.ExpiryDate < DateTime.UtcNow
                                ? DateTime.UtcNow.AddDays(1)
                                : active.ExpiryDate.AddDays(1);
                            active.IsActive = true;
                            await _subscriptionRepository.UpdateSubscriptionAsync(active);
                            await _eventPublisher.PublishAsync(new SubscriptionActivatedEvent(user.Id, active.Id, active.Plan, active.ExpiryDate, active.AmountPaid));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling iCell DataSync");
            }
        }

        // Helper for backward-compat lookup - uses repository method and handles exceptions
        private async Task<User?> _user_repository_get_by_phone(string msisdn)
        {
            try
            {
                return await _userRepository.GetUserByPhoneNumberAsync(msisdn);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error fetching user by phone {Msisdn}", msisdn);
                return null;
            }
        }

        private string GenerateTemporaryPassword()
        {
            // Generate a reasonably strong temporary password. Store clearly as requested.
            return $"IdanSure@{Guid.NewGuid().ToString("N").Substring(0,8)}";
        }

        private string HashPassword(string password)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
            }
        }

        public async Task<List<string>> GetPremiumSubscribersAsync()
        {
            return await _paymentRepository.GetUsersWithPaymentAmountAsync(2100m);
        }
        public async Task<Subscription> GetSubscriptionByCustomerRefAsync(string customerRef)
        {
            return await _subscriptionRepository.GetSubscriptionByCustomerRefAsync(customerRef);
        }
      

        public async Task SendEmailToPremiumSubscribersAsync(string subject, string body)
        {
            var premiumSubscribers = await GetPremiumSubscribersAsync();

            foreach (var email in premiumSubscribers)
            {
                await _emailService.SendEmailAsync(email, subject, body);
            }
        }

    
        public async Task<ServiceResult<SubscriptionDto>> AddAsync(Subscription subscription)
        {
            try
            {
                _logger.LogInformation($"Adding new subscription for user: {subscription.UserId}");
                await _subscriptionRepository.AddAsync(subscription);
                var subscriptionDto = MapToSubscriptionDto(subscription);
                return new ServiceResult<SubscriptionDto> { IsSuccess = true, Data = subscriptionDto, Message = "Subscription added successfully." };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error adding subscription for user: {subscription.UserId}");
                return new ServiceResult<SubscriptionDto> { IsSuccess = false, ErrorMessage = $"An error occurred while adding the subscription: {ex.Message}" };
            }
        }
        public async Task<bool> HasAnyActiveSubscriptionAsync()
        {
            try
            {
                _logger.LogInformation("Checking for any active subscription");
                var hasActiveSubscription = await _subscriptionRepository.HasAnyActiveSubscriptionAsync();
                _logger.LogInformation($"Active subscription exists: {hasActiveSubscription}");
                return hasActiveSubscription;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking for active subscriptions");
                return false;
            }
        }
        public async Task<bool> HasActiveSubscriptionByEmailAsync(string email)
        {
            var user = await _userRepository.GetUserByEmailAsync(email);
            if (user == null)
            {
                return false;
            }

            return await HasActiveSubscriptionAsync(user.Id);
        }

        public async Task<ServiceResult<SubscriptionDto>> GetActiveSubscriptionAsync(string email)
        {
            try
            {
                _logger.LogInformation($"Fetching active subscription for user: {email}");

                // Look up the user first
                var user = await _userRepository.GetUserByEmailAsync(email);
                if (user == null)
                {
                    return new ServiceResult<SubscriptionDto>
                    {
                        IsSuccess = false,
                        ErrorMessage = "User not found."
                    };
                }

                // Now get the active subscription using the user's ID
                var subscription = await _subscriptionRepository.GetActiveSubscriptionAsync(user.Id);
                if (subscription == null)
                {
                    return new ServiceResult<SubscriptionDto>
                    {
                        IsSuccess = false,
                        ErrorMessage = "No active subscription found."
                    };
                }

                var subscriptionDto = MapToSubscriptionDto(subscription);
                return new ServiceResult<SubscriptionDto>
                {
                    IsSuccess = true,
                    Data = subscriptionDto
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching active subscription for user: {email}");
                return new ServiceResult<SubscriptionDto>
                {
                    IsSuccess = false,
                    ErrorMessage = $"An error occurred while getting the active subscription: {ex.Message}"
                };
            }
        }

        public async Task<ServiceResult<List<ExpiredSubscriptionDto>>> GetExpiredSubscriptionsAsync()
        {
            try
            {
                _logger.LogInformation("Fetching all expired subscriptions");
                var expiredSubscriptions = await _subscriptionRepository.GetExpiredSubscriptionsAsync();

                if (!expiredSubscriptions.Any())
                {
                    _logger.LogWarning("No expired subscriptions found.");
                    return new ServiceResult<List<ExpiredSubscriptionDto>> { IsSuccess = true, Data = new List<ExpiredSubscriptionDto>() };
                }
                var expiredDtos = expiredSubscriptions.Select(s => new ExpiredSubscriptionDto
                {
                    PlanType = s.Plan.ToString(),
                    ExpiryDate = s.ExpiryDate,
                    Message = $"Subscription for user {s.UserId} expired on {s.ExpiryDate:d}."
                }).ToList();

                return new ServiceResult<List<ExpiredSubscriptionDto>> { IsSuccess = true, Data = expiredDtos };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching expired subscriptions");
                return new ServiceResult<List<ExpiredSubscriptionDto>> { IsSuccess = false, ErrorMessage = $"An error occurred while fetching expired subscriptions: {ex.Message}" };
            }
        }


        public async Task<ServiceResult<List<ExpiredSubscriptionDto>>> GetExpiredSubscriptionsAsync(string email)
        {
            try
            {
                var expiredSubscriptions = await _subscriptionRepository.GetExpiredSubscriptionsAsync(email);

                if (!expiredSubscriptions.Any())
                {
                    _logger.LogWarning($"No expired subscriptions found for {email}.");
                    return new ServiceResult<List<ExpiredSubscriptionDto>> { IsSuccess = true, Data = new List<ExpiredSubscriptionDto>() };
                }

                var expiredDtos = expiredSubscriptions.Select(s => new ExpiredSubscriptionDto
                {
                    PlanType = s.PlanType.ToString(),
                    ExpiryDate = s.ExpiryDate,
                    Message = $"Subscription for user {s.UserId} expired on {s.ExpiryDate:d}."
                }).ToList();

                return new ServiceResult<List<ExpiredSubscriptionDto>> { IsSuccess = true, Data = expiredDtos };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching expired subscriptions");
                return new ServiceResult<List<ExpiredSubscriptionDto>> { IsSuccess = false, ErrorMessage = $"An error occurred: {ex.Message}" };
            }
        }


        public async Task<Subscription> GetSubscriptionByIdAsync(string id)
        {
            try
            {
                _logger.LogInformation($"Fetching subscription by ID: {id}");
                var subscription = await _subscriptionRepository.GetByIdAsync(id);
                if (subscription == null)
                {
                    _logger.LogWarning($"Subscription with ID {id} not found.");
                }
                return subscription;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching subscription by ID: {id}");
                throw;
            }
        }

        public async Task<ServiceResult<SubscriptionDto>> PurchaseSubscriptionAsync(SubscriptionRequestDto request)
        {
            try
            {
                _logger.LogInformation($"Processing subscription purchase for email: {request.Email}");

                // Fetch user by email
                var user = await _userRepository.GetUserByEmailAsync(request.Email);
                if (user == null)
                {
                    _logger.LogWarning($"User not found for email: {request.Email}");
                    return new ServiceResult<SubscriptionDto> { IsSuccess = false, ErrorMessage = "User not found." };
                }

                // Check if the user already has an active subscription
                var activeSubscription = await _subscriptionRepository.GetActiveSubscriptionAsync(user.Id);
                if (activeSubscription != null)
                {
                    _logger.LogInformation($"Extending subscription for user: {user.Id}");

                    // Extend the subscription
                    var newExpiryDate = activeSubscription.ExpiryDate.AddDays(GetPlanDuration(activeSubscription.Plan));
                    activeSubscription.ExpiryDate = newExpiryDate;
                    await _subscriptionRepository.UpdateSubscriptionAsync(activeSubscription);
                    await _eventPublisher.PublishAsync(new SubscriptionActivatedEvent(user.Id, activeSubscription.Id, activeSubscription.Plan, activeSubscription.ExpiryDate, activeSubscription.AmountPaid));

                    // Send confirmation email
                    await _emailService.SendRenewalConfirmationEmailAsync(request.Email, newExpiryDate);

                    var subscriptionDto = MapToSubscriptionDto(activeSubscription);
                    return new ServiceResult<SubscriptionDto>
                    {
                        IsSuccess = true,
                        Data = subscriptionDto,
                        Message = "Subscription extended successfully."
                    };
                }

                // If the user does not have an active subscription, create a new one
                _logger.LogInformation($"Creating new subscription for user: {user.Id}");

                // Parse the plan type
                if (!Enum.TryParse<SubscriptionPlan>(request.PlanType, true, out var plan))
                {
                    return new ServiceResult<SubscriptionDto> { IsSuccess = false, ErrorMessage = $"Invalid plan type provided: {request.PlanType}" };
                }

                // Calculate the expiry date
                var expiryDate = CalculateExpiryDate(plan);
                _logger.LogInformation($"Calculated expiry date: {expiryDate} for plan: {plan}");

                // Create a new subscription
                var newSubscription = new Subscription
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    PlanType = request.PlanType,
                    AmountPaid = request.AmountPaid,
                    Currency = request.Currency,
                    StartDate = DateTime.UtcNow,
                    ExpiryDate = expiryDate,
                    IsActive = true,
                    Plan = plan,
                    Status = SubscriptionStatuses.Active,
                    TransactionId = request.TransactionId
                };

                // Save the new subscription
                await _subscriptionRepository.AddAsync(newSubscription);
                await _eventPublisher.PublishAsync(new SubscriptionActivatedEvent(user.Id, newSubscription.Id, newSubscription.Plan, newSubscription.ExpiryDate, newSubscription.AmountPaid));

                // Send confirmation email
                await _emailService.SendPurchaseConfirmationEmailAsync(
                    request.Email,
                    newSubscription.AmountPaid,
                    newSubscription.Currency,
                    newSubscription.ExpiryDate
                );

                _logger.LogInformation($"New subscription created for user: {user.Id}, Plan: {plan}, Amount: {newSubscription.AmountPaid} {newSubscription.Currency}, Expiry: {newSubscription.ExpiryDate}");

                var newSubscriptionDto = MapToSubscriptionDto(newSubscription);
                return new ServiceResult<SubscriptionDto> { IsSuccess = true, Data = newSubscriptionDto, Message = "Subscription purchased successfully." };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing subscription purchase for email: {request.Email}");
                return new ServiceResult<SubscriptionDto> { IsSuccess = false, ErrorMessage = $"An error occurred while processing the subscription: {ex.Message}" };
            }
        }



        public async Task<bool> HasActiveSubscriptionAsync(string userId)
        {
            try
            {
                _logger.LogInformation($"Checking active subscription for user: {userId}");
                var cacheKey = $"active-sub:{userId}";
                if (!_cache.TryGetValue(cacheKey, out bool hasActive))
                {
                    var subscription = await _subscriptionRepository.GetActiveSubscriptionAsync(userId);
                    hasActive = subscription != null && subscription.IsActive && subscription.ExpiryDate > DateTime.UtcNow;
                    _cache.Set(cacheKey, hasActive, TimeSpan.FromSeconds(60));
                }
                return hasActive;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking active subscription for user: {userId}");
                throw;
            }
        }

        public SubscriptionPlanDetails GetSubscriptionPlanDetails(SubscriptionPlan plan)
        {
            return new SubscriptionPlanDetails
            {
                PlanType = plan.ToString(),
                Duration = GetPlanDuration(plan),
                Price = GetPlanPrice(plan),
                Description = GetPlanDescription(plan)
            };
        }

        public async Task<ServiceResult<SubscriptionStatus>> GetSubscriptionStatusAsync(string email)
        {
            try
            {
                _logger.LogInformation($"Fetching subscription status for email: {email}");
                var user = await _userRepository.GetUserByEmailAsync(email);
                if (user == null)
                {
                    return new ServiceResult<SubscriptionStatus> { IsSuccess = false, ErrorMessage = "User not found." };
                }

                var subscription = await _subscriptionRepository.GetActiveSubscriptionAsync(user.Id);
                if (subscription == null)
                {
                    return new ServiceResult<SubscriptionStatus> { IsSuccess = true, Data = new SubscriptionStatus { IsActive = false, Message = "No active subscription found." } };
                }

                return new ServiceResult<SubscriptionStatus>
                {
                    IsSuccess = true,
                    Data = new SubscriptionStatus
                    {
                        IsActive = subscription.IsActive,
                        PlanType = subscription.Plan.ToString(),
                        ExpiryDate = subscription.ExpiryDate,
                        RemainingDays = (subscription.ExpiryDate - DateTime.UtcNow).Days,
                        Message = subscription.IsActive ? "Active subscription found." : "Inactive subscription found."
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting subscription status for email: {email}");
                return new ServiceResult<SubscriptionStatus> { IsSuccess = false, ErrorMessage = $"An error occurred while getting the subscription status: {ex.Message}" };
            }
        }


        public async Task UpdateSubscriptionAsync(Subscription subscription)
        {
            try
            {
                _logger.LogInformation($"Updating subscription for user: {subscription.UserId}");
                if (subscription == null)
                {
                    throw new ArgumentNullException(nameof(subscription), "Subscription cannot be null.");
                }

                var existingSubscription = await _subscriptionRepository.GetByIdAsync(subscription.UserId);
                if (existingSubscription == null)
                {
                    throw new InvalidOperationException($"Subscription with ID {subscription.Id} not found.");
                }

                await _subscriptionRepository.UpdateSubscriptionAsync(subscription);
                _logger.LogInformation($"Subscription updated successfully for user: {subscription.UserId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating subscription for user: {subscription.UserId}");
                throw;
            }
        }

 
        public async Task<Result> HandleFailedPaymentAsync(FailedPaymentRequest request)
        {
            try
            {
                _logger.LogInformation($"Handling failed payment for email: {request.Email}");
                var subscription = await _subscriptionRepository.GetActiveSubscriptionAsync(request.Email);
                if (subscription == null)
                {
                    return new Result { IsSuccess = false, Message = "No active subscription found." };
                }

                subscription.PaymentFailures++;
                if (subscription.PaymentFailures >= 3)
                {
                    subscription.Status = SubscriptionStatuses.Failed;
                    await _subscriptionRepository.UpdateSubscriptionAsync(subscription);
                    await _emailService.SendFailureNotificationAsync(request.Email, request.Reason);
                    return new Result { IsSuccess = false, Message = "Payment failed. Subscription marked as failed." };
                }

                var paymentSuccess = await RetryPaymentAsync(subscription);
                if (paymentSuccess)
                {
                    subscription.PaymentFailures = 0;
                    await _subscriptionRepository.UpdateSubscriptionAsync(subscription);
                    //await _emailService.SendRenewalConfirmationEmailAsync(request.Email);
                    return new Result { IsSuccess = true };
                }

                return new Result { IsSuccess = false, Message = "Payment retry failed. Subscription remains in failed state." };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error handling failed payment for email: {request.Email}");
                return new Result { IsSuccess = false, Message = $"An error occurred while handling the failed payment: {ex.Message}" };
            }
        }
     
     
        public async Task<ServiceResult<bool>> ProcessPaymentAsync(PaymentRequestDto paymentRequest)
        {
            try
            {
                _logger.LogInformation($"Processing payment for user: {paymentRequest.UserId}");
                var paymentResult = await _paymentService.ProcessPaymentResponseAsync(new PaymentResponseDto
                {
                    Status = true,
                    Data = new PaymentResponseData
                    {
                        Amount = paymentRequest.Amount,
                        Customer = new CustomerData
                        {
                            Email = (await _userRepository.GetUserByIdAsync(paymentRequest.UserId))?.Email
                        },
                        Id = paymentRequest.TransactionId,
                        Status = "success",
                        CreatedAt = paymentRequest.PaymentDate
                    }
                });

                if (paymentResult.IsSuccess)
                {
                    var user = await _userRepository.GetUserByIdAsync(paymentRequest.UserId);
                    if (user == null)
                    {
                        return new ServiceResult<bool> { IsSuccess = false, ErrorMessage = "User not found." };
                    }

                    var subscription = new Subscription
                    {
                        UserId = user.Id,
                        Plan = SubscriptionPlan.OneMonth, // Assuming 1-month subscription
                        AmountPaid = paymentRequest.Amount,
                        Currency = paymentRequest.Currency,
                        StartDate = DateTime.UtcNow,
                        ExpiryDate = DateTime.UtcNow.AddMonths(1),
                        IsActive = true,
                        TransactionId = paymentRequest.TransactionId
                    };

                    await _subscriptionRepository.AddAsync(subscription);
                    await _eventPublisher.PublishAsync(new SubscriptionActivatedEvent(user.Id, subscription.Id, subscription.Plan, subscription.ExpiryDate, subscription.AmountPaid));
                    //await _emailService.SendPurchaseConfirmationEmailAsync(user.Email, subscription.ExpiryDate);
                    await _emailService.SendPurchaseConfirmationEmailAsync(
                  user.Email,
                  subscription.AmountPaid,
                  subscription.Currency,
                  subscription.ExpiryDate
              );

                    return new ServiceResult<bool> { IsSuccess = true, Data = true, Message = "Payment processed and subscription updated successfully." };
                }
                else
                {
                    return new ServiceResult<bool> { IsSuccess = false, ErrorMessage = "Payment processing failed." };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing payment for user: {paymentRequest.UserId}");
                return new ServiceResult<bool> { IsSuccess = false, ErrorMessage = $"An error occurred while processing the payment: {ex.Message}" };
            }
        }


        public async Task<ServiceResult<SubscriptionDto>> GetMostRecentExpiredSubscriptionAsync(string email)
        {
            var expiredSubscriptionsResult = await GetExpiredSubscriptionsAsync(email);

            if (!expiredSubscriptionsResult.IsSuccess)
            {
                return new ServiceResult<SubscriptionDto>
                {
                    IsSuccess = false,
                    ErrorMessage = expiredSubscriptionsResult.ErrorMessage
                };
            }

            var mostRecentExpired = expiredSubscriptionsResult.Data?
                .OrderByDescending(s => s.ExpiryDate)
                .FirstOrDefault();

            if (mostRecentExpired == null)
            {
                return new ServiceResult<SubscriptionDto>
                {
                    IsSuccess = false,
                    ErrorMessage = "No expired subscription found."
                };
            }

            // Map ExpiredSubscriptionDto to SubscriptionDto.
            // Adjust the mapping according to your properties.
            var subscriptionDto = new SubscriptionDto
            {
                // Example mapping: adjust as needed.
                Id = Guid.NewGuid(), // or use an existing identifier if available
                UserId = "",         // Set this if available from your data
                PlanType = mostRecentExpired.PlanType,
                AmountPaid = 0,      // Set a default or map if available
                Currency = "NGN",       // Set a default or map if available
                StartDate = DateTime.MinValue, // Set a default if not applicable
                ExpiryDate = mostRecentExpired.ExpiryDate,
                IsActive = false   // Since it’s expired
            };

            return new ServiceResult<SubscriptionDto>
            {
                IsSuccess = true,
                Data = subscriptionDto
            };
        }



        public async Task<ServiceResult<PaginatedList<SubscriptionHistoryDto>>> GetSubscriptionHistoryAsync(string email, int pageNumber, int pageSize)
        {
            try
            {
                _logger.LogInformation($"Fetching subscription history for email: {email}");
                var user = await _userRepository.GetUserByEmailAsync(email);
                if (user == null)
                {
                    return new ServiceResult<PaginatedList<SubscriptionHistoryDto>>
                    {
                        IsSuccess = false,
                        ErrorMessage = "User not found."
                    };
                }

                var totalCount = await _subscriptionRepository.GetSubscriptionCountAsync(user.Id);
                var subscriptions = await _subscriptionRepository.GetSubscriptionHistoryAsync(user.Id, pageNumber, pageSize);

                var historyDtos = subscriptions.Select(s => new SubscriptionHistoryDto
                {
                    PlanType = s.Plan.ToString(),
                    AmountPaid = s.AmountPaid,
                    Currency = s.Currency,
                    StartDate = s.StartDate,
                    ExpiryDate = s.ExpiryDate
                }).ToList();

                var paginatedResult = new PaginatedList<SubscriptionHistoryDto>(historyDtos, totalCount, pageNumber, pageSize);

                return new ServiceResult<PaginatedList<SubscriptionHistoryDto>>
                {
                    IsSuccess = true,
                    Data = paginatedResult
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching subscription history for email: {email}");
                return new ServiceResult<PaginatedList<SubscriptionHistoryDto>>
                {
                    IsSuccess = false,
                    ErrorMessage = $"An error occurred while fetching subscription history: {ex.Message}"
                };
            }
        }



        public async Task<ServiceResult<bool>> NotifyExpiredSubscriptionsAsync()
        {
            try
            {
                _logger.LogInformation("Notifying expired subscriptions");
                var expiredSubscriptions = await _subscriptionRepository.GetExpiredSubscriptionsAsync();

                foreach (var subscription in expiredSubscriptions)
                {
                    var user = await _userRepository.GetUserByIdAsync(subscription.UserId);
                    if (user != null)
                    {
                        await _emailService.SendEmailAsync(
                            user.Email,
                            "Subscription Expired",
                            $"Your {subscription.Plan} subscription has expired. Please renew to continue enjoying our services."
                        );
                    }
                }

                return new ServiceResult<bool> { IsSuccess = true, Data = true, Message = "Expired subscriptions notified successfully." };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error notifying expired subscriptions");
                return new ServiceResult<bool> { IsSuccess = false, ErrorMessage = $"An error occurred while notifying expired subscriptions: {ex.Message}" };
            }
        }

        private async Task<bool> RetryPaymentAsync(Subscription subscription)
        {
            // This is a placeholder for actual payment retry logic
            await Task.Delay(1000); // Simulate some delay for retry
            var random = new Random();
            return random.Next(0, 2) == 1; // 50% chance of retry success
        }


        private int GetPlanDuration(SubscriptionPlan plan)
        {
            return plan switch
            {
                SubscriptionPlan.OneDay => 1,
                SubscriptionPlan.OneWeek => 7,
                SubscriptionPlan.OneMonth => 31,
                _ => throw new ArgumentException("Invalid subscription plan")
            };
        }

        private decimal GetPlanPrice(SubscriptionPlan plan)
        {
            return plan switch
            {
                SubscriptionPlan.OneDay => 100,
                SubscriptionPlan.OneWeek => 650,
                SubscriptionPlan.OneMonth => 2100,
                _ => throw new ArgumentException("Invalid subscription plan")
            };
        }

        private string GetPlanDescription(SubscriptionPlan plan)
        {
            return plan switch
            {
                SubscriptionPlan.OneDay => "24-hour access to all premium features",
                SubscriptionPlan.OneWeek => "7-day unlimited access to all premium features",
                SubscriptionPlan.OneMonth => "Full month of unlimited access to all premium features",
                _ => throw new ArgumentException("Invalid subscription plan")
            };
        }
        private SubscriptionPlan DeterminePlanType(decimal amount)
        {
            if (amount >= 2100) return SubscriptionPlan.OneMonth;
            if (amount >= 650) return SubscriptionPlan.OneWeek;
            return SubscriptionPlan.OneDay;
        }

        private DateTime CalculateExpiryDate(SubscriptionPlan plan)
        {
            return plan switch
            {
                SubscriptionPlan.OneDay => DateTime.UtcNow.AddDays(1),
                SubscriptionPlan.OneWeek => DateTime.UtcNow.AddDays(7),
                SubscriptionPlan.OneMonth => DateTime.UtcNow.AddMonths(1),
                _ => throw new ArgumentException($"Invalid subscription plan: {plan}")
            };
        }
        public async Task<ServiceResult<bool>> NotifyExpiringSubscriptionsAsync()
        {
            try
            {
                _logger.LogInformation("Checking for expiring subscriptions");
                var expiringSubscriptions = await _subscriptionRepository.GetExpiringSubscriptionsAsync();

                foreach (var subscription in expiringSubscriptions)
                {
                    var user = await _userRepository.GetUserByIdAsync(subscription.UserId);
                    if (user != null)
                    {
                        int daysUntilExpiry = (int)(subscription.ExpiryDate - DateTime.UtcNow).TotalDays;
                        string subject = "Your Subscription is Expiring Soon";
                        string body = $"Your {subscription.Plan} subscription will expire in {daysUntilExpiry} day(s). Please renew to continue enjoying our services.";

                        if (subscription.Plan == SubscriptionPlan.OneDay)
                        {
                            // For one-day subscribers, send notification when less than 6 hours remain
                            if (daysUntilExpiry < 0.25)
                            {
                                await _emailService.SendEmailAsync(user.Email, subject, body);
                            }
                        }
                        else
                        {
                            // For other subscribers, send notification 3 days before expiry
                            if (daysUntilExpiry <= 3)
                            {
                                await _emailService.SendEmailAsync(user.Email, subject, body);
                            }
                        }
                    }
                }

                return new ServiceResult<bool> { IsSuccess = true, Data = true, Message = "Expiring subscriptions notified successfully." };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error notifying expiring subscriptions");
                return new ServiceResult<bool> { IsSuccess = false, ErrorMessage = $"An error occurred while notifying expiring subscriptions: {ex.Message}" };
            }
        }

        public async Task<ServiceResult<SubscriptionDto>> RenewOrExtendSubscriptionAsync(SubscriptionRenewalRequestDto request)
{
    try
    {
        _logger.LogInformation($"Processing subscription renewal/extension for email: {request.Email}");

        // Fetch user by email
        var user = await _userRepository.GetUserByEmailAsync(request.Email);
        if (user == null)
        {
            return new ServiceResult<SubscriptionDto>
            {
                IsSuccess = false,
                ErrorMessage = "User not found."
            };
        }

        // Check if the user has an active subscription
        var activeSubscription = await _subscriptionRepository.GetActiveSubscriptionAsync(user.Id);
        if (activeSubscription == null)
        {
            return new ServiceResult<SubscriptionDto>
            {
                IsSuccess = false,
                ErrorMessage = "No active subscription found. Please purchase a new subscription."
            };
        }

        // Calculate the new expiry date based on the renewal period
        var newExpiryDate = activeSubscription.ExpiryDate.AddDays(request.RenewalDays);

        // Update the subscription's expiry date
        activeSubscription.ExpiryDate = newExpiryDate;
        await _subscriptionRepository.UpdateSubscriptionAsync(activeSubscription);

        // Send confirmation email
        await _emailService.SendRenewalConfirmationEmailAsync(request.Email, newExpiryDate);

        var subscriptionDto = MapToSubscriptionDto(activeSubscription);
        return new ServiceResult<SubscriptionDto>
        {
            IsSuccess = true,
            Data = subscriptionDto,
            Message = "Subscription renewed/extended successfully."
        };
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, $"Error renewing/extending subscription for email: {request.Email}");
        return new ServiceResult<SubscriptionDto>
        {
            IsSuccess = false,
            ErrorMessage = $"An error occurred while renewing/extending the subscription: {ex.Message}"
        };
    }
}


        private SubscriptionDto MapToSubscriptionDto(Subscription subscription)
        {
            return new SubscriptionDto
            {
                Id = subscription.Id,
                UserId = subscription.UserId,
                PlanType = subscription.PlanType,
                AmountPaid = subscription.AmountPaid,
                Currency = subscription.Currency,
                StartDate = subscription.StartDate,
                ExpiryDate = subscription.ExpiryDate,
                IsActive = subscription.IsActive
            };
        }
    }
}

