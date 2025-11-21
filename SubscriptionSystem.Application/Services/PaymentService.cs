using Microsoft.Extensions.Configuration;
using SubscriptionSystem.Application.DTOs;
using SubscriptionSystem.Application.Interfaces;
using SubscriptionSystem.Domain.Entities;
using System.Text.Json;
using System.Text;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using SubscriptionSystem.Application.Common;
using SubscriptionSystem.Domain.Entities.SubscriptionSystem.Domain.Entities;
using System.Linq.Expressions;
using static SubscriptionSystem.Application.DTOs.AdminDashboardDto;



namespace SubscriptionSystem.Application.Services
{
    public class PaymentService : IPaymentService
    {

        private readonly ITransactionRepository _transactionRepository;
     
        private readonly IUserRepository _userRepository;
        private readonly ISubscriptionRepository _subscriptionRepository;
        private readonly IEmailService _emailService;
        private readonly IVerifiedEmailRepository _verifiedEmailRepository;
        private readonly IPaymentRepository _paymentRepository;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly HttpClient _paystackHttpClient;
        private readonly string _paystackSecretKey;

        public PaymentService(
     IUserRepository userRepository,
     ISubscriptionRepository subscriptionRepository,
     IEmailService emailService,
     IVerifiedEmailRepository verifiedEmailRepository,
     IPaymentRepository paymentRepository,
     IConfiguration configuration,
     IHttpClientFactory httpClientFactory,
     ITransactionRepository transactionRepository)
        {
            _transactionRepository = transactionRepository;
            _userRepository = userRepository;
            _subscriptionRepository = subscriptionRepository;
            _emailService = emailService;
            _verifiedEmailRepository = verifiedEmailRepository;
            _paymentRepository = paymentRepository;
            _configuration = configuration;

          
            // Paystack client setup - SINGLE CONFIGURATION
            _paystackHttpClient = httpClientFactory.CreateClient("PaystackClient");
            _paystackSecretKey = _configuration["Paystack:SecretKey"] ?? throw new ArgumentException("Paystack:SecretKey is not configured");
            _httpClient = httpClientFactory?.CreateClient("Credo")
         ?? throw new ArgumentNullException("Credo HttpClient is not configured.");

            _paystackHttpClient.BaseAddress = new Uri(_configuration["Paystack:BaseUrl"] ?? "https://api.paystack.co/");
            _paystackHttpClient.DefaultRequestHeaders.Clear();
            _paystackHttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _paystackHttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _paystackSecretKey);

            // Log partial key for debugging (first 4 and last 4 characters)
            var keyFirstChars = _paystackSecretKey.Substring(0, Math.Min(4, _paystackSecretKey.Length));
            var keyLastChars = _paystackSecretKey.Length > 4
                ? _paystackSecretKey.Substring(_paystackSecretKey.Length - 4)
                : "";

            // Ensure headers are set
            if (!_httpClient.DefaultRequestHeaders.Contains("Authorization"))
            {
                var apiKey = _configuration["Credo:ApiKey"];
                if (string.IsNullOrEmpty(apiKey))
                    throw new ArgumentException("CoralPay API key is missing in configuration.");

                _httpClient.DefaultRequestHeaders.Add("Authorization", apiKey);
            }
        }
        public async Task<ServiceResult<PaymentInitializationResponseDto>> InitializePaymentAsync(PaymentInitializationDto request)
        {
            try
            {
                // Validate required fields from the DTO
                if (string.IsNullOrEmpty(request.CustomerPhoneNumber))
                    throw new ArgumentException("Customer phone number is required.");

                // Generate a unique reference if not provided
                string uniqueReference = string.IsNullOrEmpty(request.Reference)
                    ? GenerateUniqueReference()
                    : request.Reference;

                // Construct metadata
                var metadata = new
                {
                    bankAccount = request.BankAccount,
                    customFields = request.CustomFields?.Select(cf => new
                    {
                        variable_name = cf.VariableName,
                        value = cf.Value,
                        display_name = cf.DisplayName
                    }).ToList()
                };

                // Build the payload
                var payload = new
                {
                    amount = request.Amount * 100,
                    bearer = request.Bearer,
                    callbackUrl = request.CallbackUrl,
                    channels = new[] { "card", "bank" },
                    currency = "NGN",
                    customerFirstName = request.CustomerFirstName,
                    customerLastName = request.CustomerLastName,
                    customerPhoneNumber = request.CustomerPhoneNumber,
                    email = request.Email,
                    metadata = metadata,
                    reference = uniqueReference
                };

                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                };

                var json = JsonSerializer.Serialize(payload, jsonOptions);
                Console.WriteLine($"Request payload: {json}");

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                using var requestMessage = new HttpRequestMessage(HttpMethod.Post, "transaction/initialize")
                {
                    Content = content
                };

                // Set Authorization header if missing
                if (!_httpClient.DefaultRequestHeaders.Contains("Authorization"))
                {
                    _httpClient.DefaultRequestHeaders.Add("Authorization", _configuration["Credo:ApiKey"]);
                }

                var response = await _httpClient.SendAsync(requestMessage);
                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"API Response: {responseContent}");

                if (response.IsSuccessStatusCode)
                {
                    // Deserialize the response
                    var result = JsonSerializer.Deserialize<ApiResponse<PaymentInitializationResponseDto>>(
                        responseContent,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    // Fix: Use proper null checks for both result and result.Data
                    if (result != null && result.Status == 200 && result.Data != null)
                    {
                        return new ServiceResult<PaymentInitializationResponseDto>
                        {
                            IsSuccess = true,
                            Data = result.Data,
                            Message = result.Message
                        };
                    }
                    else
                    {
                        // Handle null result or missing data
                        return new ServiceResult<PaymentInitializationResponseDto>
                        {
                            IsSuccess = false,
                            ErrorMessage = result?.Message ?? "API returned an invalid or empty response."
                        };
                    }
                }
                else
                {
                    // Handle HTTP errors
                    var errorResult = JsonSerializer.Deserialize<ApiResponse<object>>(
                        responseContent,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    return new ServiceResult<PaymentInitializationResponseDto>
                    {
                        IsSuccess = false,
                        ErrorMessage = errorResult?.Message ?? $"HTTP Error: {response.StatusCode}"
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex}");
                return new ServiceResult<PaymentInitializationResponseDto>
                {
                    IsSuccess = false,
                    ErrorMessage = $"Initialization failed: {ex.Message}"
                };
            }
        }
        public async Task<Result<StandardizedTransaction>> ProcessUnifiedWebhookAsync(PaystackWebhookDto webhookData, string gateway)
        {
            try
            {
                if (gateway.ToLower() != "paystack")
                {
                    return Result<StandardizedTransaction>.Failure("Invalid gateway for Paystack webhook processing");
                }

                // Process the Paystack webhook directly
                var paystackResult = await ProcessPaystackWebhook(webhookData);
                if (!paystackResult.IsSuccess) return paystackResult;

                // Handle the transaction processing
                return await HandleTransactionProcessing(paystackResult.Data);
            }
            catch (Exception ex)
            {
                return Result<StandardizedTransaction>.Failure($"Error processing Paystack webhook: {ex.Message}");
            }
        }
        public async Task<Result<StandardizedTransaction>> ProcessPaystackWebhook(PaystackWebhookDto webhookData)
        {
            try
            {
                if (webhookData?.Data == null)
                {
                    return Result<StandardizedTransaction>.Failure("Invalid Paystack webhook data");
                }

                // Convert amount from kobo to naira (Paystack uses kobo)
                decimal amountInNaira = webhookData.Data.Amount / 100m;

                var transaction = new StandardizedTransaction
                {
                    PaymentGateway = "Paystack",
                    ExternalTransactionId = webhookData.Data.Reference,
                    Amount = amountInNaira,
                    UserId = webhookData.Data.Metadata?.UserId ?? webhookData.Data.Customer?.Email ?? "unknown",
                    Email = webhookData.Data.Customer?.Email ?? "unknown",
                    Status = webhookData.Data.Status == "success" ? "Completed" : "Failed",
                    PlanType = webhookData.Data.Metadata?.PlanType ?? GetPlanType(amountInNaira),
                    RawPayload = JsonSerializer.Serialize(webhookData),
                    CreatedAt = DateTime.UtcNow,
                    CompletedAt = webhookData.Data.Status == "success" ? DateTime.UtcNow : (DateTime?)null
                };

                return Result<StandardizedTransaction>.Success(transaction);
            }
            catch (Exception ex)
            {
                return Result<StandardizedTransaction>.Failure($"Error processing Paystack webhook: {ex.Message}");
            }
        }


     public async Task<ServiceResult<PaystackInitializeResponseDto>> InitializePaystackPaymentAsync(PaystackInitializeRequestDto request)
{
    int maxRetries = 3;
    int currentRetry = 0;
    TimeSpan delay = TimeSpan.FromSeconds(2);

    while (true)
    {
        try
        {
            // Generate a unique reference if not provided
            if (string.IsNullOrEmpty(request.Reference) || request.Reference == "string")
            {
                request.Reference = $"PS_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid().ToString("N").Substring(0, 8)}";
            }

            // Convert naira to kobo (1 naira = 100 kobo)
            int amountInKobo = (int)(request.Amount * 100);

            // Create a proper payload with Paystack's expected field names
            var payload = new
            {
                email = request.Email,
                amount = amountInKobo, // This must be in kobo for Paystack
                reference = request.Reference,
                callback_url = request.CallbackUrl,
                metadata = request.Metadata
            };

            var json = JsonSerializer.Serialize(payload);
            Console.WriteLine($"Payload: {json}");

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Log attempt
            Console.WriteLine($"Attempt {currentRetry + 1} of {maxRetries + 1} to initialize Paystack payment");

            var response = await _paystackHttpClient.PostAsync("/transaction/initialize", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            Console.WriteLine($"Response: {responseContent}");

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<PaystackInitializeResponseDto>(responseContent, 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                
                if (result?.Status == true && result.Data != null)
                {
                    // Keep using object initialization since that's what works in your code
                    return new ServiceResult<PaystackInitializeResponseDto>
                    {
                        IsSuccess = true,
                        Data = result,
                        Message = result.Message
                    };
                }
            }

            // Keep using object initialization for error cases
            return new ServiceResult<PaystackInitializeResponseDto>
            {
                IsSuccess = false,
                ErrorMessage = $"Failed to initialize payment. Status code: {response.StatusCode}. Response: {responseContent}"
            };
        }
        catch (HttpRequestException ex) when (currentRetry < maxRetries)
        {
            // Log the exception
            Console.WriteLine($"Attempt {currentRetry + 1} failed with HttpRequestException: {ex.Message}");
            currentRetry++;
            await Task.Delay(delay);
            delay = TimeSpan.FromSeconds(Math.Pow(2, currentRetry) * 2);
            continue;
        }
        catch (TaskCanceledException ex) when (currentRetry < maxRetries)
        {
            // Handle timeout exceptions
            Console.WriteLine($"Attempt {currentRetry + 1} timed out: {ex.Message}");
            currentRetry++;
            await Task.Delay(delay);
            delay = TimeSpan.FromSeconds(Math.Pow(2, currentRetry) * 2);
            continue;
        }
        catch (Exception ex)
        {
            // Log the exception
            Console.WriteLine($"Unhandled exception in InitializePaystackPaymentAsync: {ex}");
            return new ServiceResult<PaystackInitializeResponseDto>
            {
                IsSuccess = false,
                ErrorMessage = $"An unexpected error occurred while initializing payment: {ex.Message}"
            };
        }
    }
}
        public async Task<ServiceResult<PaystackVerifyResponseDto>> VerifyPaystackPaymentAsync(string reference)
        {
            try
            {
                var response = await _paystackHttpClient.GetAsync($"/transaction/verify/{reference}");
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<PaystackVerifyResponseDto>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (result?.Status == true)
                    {
                        // If payment is successful, update user subscription
                        if (result.Data.Status == "success")
                        {
                            var user = await _userRepository.GetUserByEmailAsync(result.Data.Customer.Email);
                            if (user != null)
                            {
                                user.HasActiveSubscription = true;
                                await _userRepository.UpdateUserAsync(user);

                                // Get plan type from metadata or determine from amount
                                var planType = result.Data.Metadata?.PlanType ?? GetPlanType(result.Data.Amount / 100m);

                                // Create standardized transaction to track gateway
                                var transaction = new StandardizedTransaction
                                {
                                    PaymentGateway = "Paystack",
                                    ExternalTransactionId = reference,
                                    Amount = result.Data.Amount / 100m, // Convert from kobo to naira
                                    UserId = user.Id,
                                    Email = result.Data.Customer.Email,
                                    Status = "Completed",
                                    PlanType = planType,
                                    RawPayload = JsonSerializer.Serialize(result),
                                    CreatedAt = DateTime.UtcNow,
                                    CompletedAt = DateTime.UtcNow
                                };

                                // Save transaction to track gateway used
                                await _transactionRepository.AddAsync(transaction);

                                // Update subscription status
                                await UpdateSubscriptionStatus(result.Data.Customer.Email, planType);

                                // Send confirmation email
                                await SendPaymentConfirmationEmail(result.Data.Customer.Email, transaction.Amount, CalculateExpiryDate(Enum.Parse<SubscriptionPlan>(planType)));
                            }
                        }

                        return new ServiceResult<PaystackVerifyResponseDto>
                        {
                            IsSuccess = true,
                            Data = result,
                            Message = "Payment verification successful"
                        };
                    }
                }

                return new ServiceResult<PaystackVerifyResponseDto>
                {
                    IsSuccess = false,
                    ErrorMessage = $"Failed to verify payment. Status code: {response.StatusCode}. Response: {responseContent}"
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<PaystackVerifyResponseDto>
                {
                    IsSuccess = false,
                    ErrorMessage = $"An unexpected error occurred while verifying payment: {ex.Message}"
                };
            }
        }

        public bool VerifyPaystackWebhookSignature(string payload, string signature)
        {
            try
            {
                if (string.IsNullOrEmpty(_paystackSecretKey) || string.IsNullOrEmpty(signature))
                    return false;

                using (var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(_paystackSecretKey)))
                {
                    var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
                    var requestHash = signature.Replace("sha512=", "");

                    return BitConverter.ToString(computedHash).Replace("-", "").ToLower() == requestHash.ToLower();
                }
            }
            catch
            {
                return false;
            }
        }
        private async Task<Result<StandardizedTransaction>> ProcessPaystackWebhookFromUnified(PaymentWebhookDto webhookData)
        {
            try
            {
                // Convert PaymentWebhookDto to PaystackWebhookDto
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var paystackData = JsonSerializer.Deserialize<PaystackWebhookDto>(
                    JsonSerializer.Serialize(webhookData),
                    options
                );

                if (paystackData == null)
                {
                    return Result<StandardizedTransaction>.Failure("Invalid Paystack webhook data");
                }

                // Now call the original method with the converted data
                return await ProcessPaystackWebhook(paystackData);
            }
            catch (Exception ex)
            {
                return Result<StandardizedTransaction>.Failure($"Error processing Paystack webhook: {ex.Message}");
            }
        }
        public async Task<Result<StandardizedTransaction>> ProcessUnifiedWebhookAsync(PaymentWebhookDto webhookData, string gateway)
        {
            try
            {
                StandardizedTransaction transaction;
                switch (gateway.ToLower())
                {
                    case "credo":
                        var credoResult = await ProcessCredoWebhook(webhookData);
                        if (!credoResult.IsSuccess) return credoResult;
                        transaction = credoResult.Data;
                        break;
                    case "paystack":
                        var paystackResult = await ProcessPaystackWebhookFromUnified(webhookData);
                        if (!paystackResult.IsSuccess) return paystackResult;
                        transaction = paystackResult.Data;
                        break;

                    case "alatpay":  // Add this case
                        var alatResult = await ProcessAlatPayWebhook(webhookData);
                        if (!alatResult.IsSuccess) return alatResult;
                        transaction = alatResult.Data;
                        break;

                    case "coralpay":
                        var coralResult = await ProcessCoralPayWebhook(webhookData);
                        if (!coralResult.IsSuccess) return coralResult;
                        transaction = coralResult.Data;
                        break;

                    default:
                        return Result<StandardizedTransaction>.Failure("Unsupported payment gateway");
                }

                return await HandleTransactionProcessing(transaction);
            }
            catch (Exception ex)
            {
            
                return Result<StandardizedTransaction>.Failure($"Error processing webhook: {ex.Message}");
            }
        }

        public async Task ProcessUnifiedWebhookAsync(PaymentNotificationDto notification)
        {
            try
            {
    
                var result = await ProcessUnifiedWebhookAsync(notification, "coralpay");
                if (!result.IsSuccess)
                {
                 
                }
            }
            catch (Exception ex)
            {
            }
        }



        public async Task<Result<StandardizedTransaction>> ProcessUnifiedWebhookAsync(PaymentNotificationDto notification, string gateway)
        {
            try
            {
                if (gateway.ToLower() != "coralpay")
                {
                    return Result<StandardizedTransaction>.Failure("Invalid gateway for this method");
                }

                // Process CoralPay webhook
                var coralResult = await ProcessCoralPayWebhookAsync(notification);
                if (!coralResult.IsSuccess) return coralResult;

                return await HandleTransactionProcessing(coralResult.Data);
            }
            catch (Exception ex)
            {
               
                return Result<StandardizedTransaction>.Failure($"Error processing CoralPay webhook: {ex.Message}");
            }
        }


        // Add this method to your PaymentService class
        public async Task<Result<StandardizedTransaction>> ProcessUnifiedWebhookAsync(WebhookPayloadDto webhookData, string gatewayName)
        {
            try
            {
                if (gatewayName.ToLower() != "credo")
                {
                    return Result<StandardizedTransaction>.Failure("Invalid gateway for Credo webhook processing");
                }

                var transaction = new StandardizedTransaction
                {
                    PaymentGateway = "credo",
                    ExternalTransactionId = webhookData.Data.TransRef,
                    Amount = webhookData.Data.Amount, // Already in naira
                    UserId = webhookData.Data.CustomerId ?? "unknown",
                    Email = webhookData.Data.Customer?.CustomerEmail ?? "unknown",
                    Status = webhookData.Data.Status == 1 ? "Completed" : "Failed",
                    PlanType = GetPlanType(webhookData.Data.Amount),
                    RawPayload = JsonSerializer.Serialize(webhookData)
                };

                return await HandleTransactionProcessing(transaction);
            }
            catch (Exception ex)
            {
                return Result<StandardizedTransaction>.Failure($"Error processing Credo webhook: {ex.Message}");
            }
        }

       

        private string GetPlanType(decimal amount)
        {
            return amount switch
            {
                100m => "OneDay",
                650m => "OneWeek",
                2100m => "OneMonth",
                _ => "Unknown Plan"
            };
        }

        public async Task<Result<StandardizedTransaction>> ProcessCoralPayWebhookAsync(PaymentNotificationDto notification)
        {
            try
            {
                var transaction = new StandardizedTransaction
                {
                    PaymentGateway = "CoralPay",
                    ExternalTransactionId = notification.PaymentReference ?? "unknown",
                    Amount = notification.Amount,
                    UserId = notification.CustomerRef ?? "unknown",
                    Email = notification.MobileNumber ?? "unknown",
                    Status = notification.ResponseCode == "00" ? "Completed" : "Failed",
                    PlanType = GetPlanType(notification.Amount), // Ensure this method exists
                    RawPayload = JsonSerializer.Serialize(notification)
                };

                return Result<StandardizedTransaction>.Success(transaction);
            }
            catch (Exception ex)
            {
              
                return Result<StandardizedTransaction>.Failure("Error processing CoralPay webhook");
            }
        }

        

        private async Task<Result<StandardizedTransaction>> ProcessCoralPayWebhook(PaymentWebhookDto webhookData)
        {
            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var coralPayData = JsonSerializer.Deserialize<PaymentNotificationDto>(
                    JsonSerializer.Serialize(webhookData),
                    options
                );

                if (coralPayData == null)
                {
                   
                    return Result<StandardizedTransaction>.Failure("Invalid CoralPay webhook data");
                }

                var transaction = new StandardizedTransaction
                {
                    PaymentGateway = "CoralPay",
                    ExternalTransactionId = coralPayData.PaymentReference ?? "unknown",
                    Amount = coralPayData.Amount / 100m, // Convert from kobo if needed
                    UserId = coralPayData.CustomerRef ?? "unknown",
                    Email = "unknown", // CoralPay DTO does not contain email
                    Status = coralPayData.ResponseCode == "00" ? "Completed" : "Failed", // Assuming "00" means success
                    PlanType = GetPlanType(coralPayData.Amount),
                    RawPayload = JsonSerializer.Serialize(coralPayData)
                };

                return Result<StandardizedTransaction>.Success(transaction);
            }
            catch (Exception ex)
            {
               
                return Result<StandardizedTransaction>.Failure("Error processing CoralPay webhook");
            }
        }


        private async Task<Result<StandardizedTransaction>> ProcessCredoWebhook(PaymentWebhookDto webhookData)
        {
            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var credoData = JsonSerializer.Deserialize<CredoWebhookPayloadDto>(
                    JsonSerializer.Serialize(webhookData),
                    options
                );

                if (credoData.data == null)
                {
               
                    return Result<StandardizedTransaction>.Failure("Invalid Credo webhook data");
                }

                var transaction = new StandardizedTransaction
                {
                    PaymentGateway = "Credo",
                    ExternalTransactionId = credoData.data.reference ?? "unknown",
                    Amount = credoData.data.amount, // Convert from kobo
                    UserId = credoData.data.customerId ?? "unknown",
                    Email = credoData.data.customer?.customerEmail ?? "unknown",
                    Status = credoData.data.status == 1 ? "Completed" : "Failed",
                    PlanType = GetPlanType( credoData.data.amount), // Convert amount from kobo
                    RawPayload = JsonSerializer.Serialize(credoData)
                };

                return Result<StandardizedTransaction>.Success(transaction);
            }
            catch (Exception ex)
            {
                return Result<StandardizedTransaction>.Failure("Error processing Credo webhook");
            }
        }

        private async Task<Result<StandardizedTransaction>> ProcessAlatPayWebhook(PaymentWebhookDto webhookData)
        {
            try
            {
                if (webhookData?.Value?.Data == null)
                {
                   
                    return Result<StandardizedTransaction>.Failure("Invalid AlatPay webhook data");
                }

                var transaction = new StandardizedTransaction
                {
                    PaymentGateway = "AlatPay",
                    ExternalTransactionId = webhookData.Value.Data.Id,
                    Amount = webhookData.Value.Data.Amount,
                    UserId = webhookData.Value.Data.Customer?.Id ?? "unknown",
                    Email = webhookData.Value.Data.Customer?.Email ?? "unknown",
                    Status = webhookData.Value.Status ? "Completed" : "Failed",
                    PlanType = webhookData.Value.Data.PlanType,
                    RawPayload = JsonSerializer.Serialize(webhookData)
                };

                return await HandleTransactionProcessing(transaction);
            }
            catch (Exception ex)
            {
                
                return Result<StandardizedTransaction>.Failure("Error processing AlatPay webhook");
            }
        }


        private async Task<Result<StandardizedTransaction>> HandleTransactionProcessing(StandardizedTransaction transaction)
        {
            try
            {
                if (transaction.Status == "Completed")
                {
                    transaction.CompletedAt = DateTime.UtcNow;
                }

                var existingTransaction = await _transactionRepository.GetByExternalIdAsync(
                    transaction.ExternalTransactionId,
                    transaction.PaymentGateway
                );

                if (existingTransaction != null)
                {
                    return await HandleExistingTransaction(existingTransaction, transaction);
                }

                
                var savedTransaction = await _transactionRepository.AddAsync(transaction);

                if (transaction.Status == "Completed")
                {
                    await UpdateSubscriptionFromTransaction(savedTransaction);
                }

                return Result<StandardizedTransaction>.Success(savedTransaction);
            }
            catch (Exception ex)
            {
               
                return Result<StandardizedTransaction>.Failure("Transaction processing failed");
            }
        }
       

        
        private async Task<Result<StandardizedTransaction>> HandleExistingTransaction(StandardizedTransaction existingTransaction, StandardizedTransaction newTransaction)
        {
           

            // Ensure the latest status is recorded
            if (newTransaction.Status == "Completed" && existingTransaction.Status != "Completed")
            {
                existingTransaction.Status = "Completed";
                existingTransaction.CompletedAt = DateTime.UtcNow;
            }

            // Update transaction details if needed
            existingTransaction.Amount = newTransaction.Amount;
            existingTransaction.RawPayload = newTransaction.RawPayload;

            await _transactionRepository.UpdateAsync(existingTransaction);

            return Result<StandardizedTransaction>.Success(existingTransaction);
        }

        private async Task UpdateSubscriptionFromTransaction(StandardizedTransaction transaction)
        {
            // Logic to update user subscription based on transaction
            // This would typically involve:
            // 1. Determining subscription duration from plan type
            // 2. Creating or extending subscription
            // 3. Updating user subscription status

            // Example implementation:
            int daysToAdd = 0;
            switch (transaction.PlanType)
            {
                case "OneDay":
                    daysToAdd = 1;
                    break;
                case "OneWeek":
                    daysToAdd = 7;
                    break;
                case "OneMonth":
                    daysToAdd = 30;
                    break;
                default:
                   
                    return;
            }

            var subscription = new Subscription
            {
                UserId = transaction.UserId,
                Email = transaction.Email,
                PlanType = transaction.PlanType,
                AmountPaid = transaction.Amount,
                PaymentGateway = transaction.PaymentGateway,
                TransactionId = transaction.Id,
                StartDate = DateTime.UtcNow,
                ExpiryDate = DateTime.UtcNow.AddDays(daysToAdd),
                IsActive = true
            };

            await _subscriptionRepository.AddOrUpdateSubscriptionAsync(subscription);
        }

        private string ExtractPlanTypeFromMetadata(string metadata)
        {
            try
            {
                if (string.IsNullOrEmpty(metadata))
                    return "Unknown";

                var metadataObj = JsonSerializer.Deserialize<Dictionary<string, string>>(metadata);
                if (metadataObj.TryGetValue("plan", out var plan))
                {
                    switch (plan.ToLower())
                    {
                        case "daily":
                            return "OneDay";
                        case "weekly":
                            return "OneWeek";
                        case "monthly":
                            return "OneMonth";
                        default:
                            return plan;
                    }
                }

                return "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }

        private string ExtractUserIdFromAlatPayData(PaymentWebhookDto data)
        {
            // Implementation depends on how user ID is stored in AlatPay data
            // This is a placeholder - adjust based on your actual data structure
            try
            {
                if (data.Value.Data.Customer != null)
                {
                    return data.Value.Data.Customer.Id; // Use uppercase 'Value'
                }

                // If no direct user ID is available, you might use other identifiers
                return data.Value.Data.Id; // Use the transaction ID as a fallback
            }
            catch (Exception ex)
            {
                
                return "Unknown";
            }
        }

        private string ExtractEmailFromCoralPayData(PaymentNotificationDto data)
        {
            // Since PaymentNotificationDto doesn't have a direct email property,
            // we need to extract it from available data
            if (data.CustomerRef != null )
            {
                return data.CustomerRef;
            }

            // If no email is available, return a placeholder or the CustomerRef
            return data.CustomerRef ?? "Unknown";
        }

        private string ExtractPlanTypeFromAmount(decimal amount)
        {
            // Determine plan type based on amount
            // This assumes fixed pricing for plans
            if (amount == 100)
                return "OneDay";
            else if (amount == 650)
                return "OneWeek";
            else if (amount == 2100)
                return "OneMonth";
            else
                return "Unknown";
        }

        private string GetPlanType(string metadata, decimal amount)
        {
            switch (amount)
            {
                case 100m:
                    return "Daily";
                case 650m:
                    return "Weekly";
                case 2100m:
                    return "Premium";
                default:

                    return "Unknown";
            }
        }



        public async Task<Result<TransactionReportDto>> GetTransactionReportAsync(
    DateTime? startDate, DateTime? endDate, string gateway, string planType, int page, int pageSize)
        {
            try
            {
                // Check if repository is initialized
                if (_transactionRepository == null)
                {
                   
                    return Result<TransactionReportDto>.Failure("Transaction repository is not initialized");
                }

                // Build query predicate - IMPORTANT CHANGE: Don't filter by CompletedAt for initial query
                Expression<Func<StandardizedTransaction, bool>> predicate = t => true;

                // Apply date filters to CreatedAt instead of CompletedAt to include all transactions
                if (startDate.HasValue)
                    predicate = predicate.And(t => t.CreatedAt >= startDate.Value);

                if (endDate.HasValue)
                    predicate = predicate.And(t => t.CreatedAt <= endDate.Value);

                // Apply gateway filter (case-insensitive)
                if (!string.IsNullOrEmpty(gateway))
                    predicate = predicate.And(t => t.PaymentGateway != null && t.PaymentGateway.ToLower() == gateway.ToLower());

                // Apply plan type filter (case-insensitive)
                if (!string.IsNullOrEmpty(planType))
                    predicate = predicate.And(t => t.PlanType != null && t.PlanType.ToLower() == planType.ToLower());

                // Get total count for pagination
                var totalCount = await _transactionRepository.CountAsync(predicate);

                // Get transactions for current page - Order by CreatedAt instead of CompletedAt
                var transactions = await _transactionRepository.GetPagedAsync(
                    page, pageSize, predicate, q => q.OrderByDescending(t => t.CreatedAt));

                // Check if transactions is null
                if (transactions == null)
                {
                    
                    transactions = new List<StandardizedTransaction>(); // Use empty list instead of null
                }

                // Map to DTOs with null checks
                var transactionDtos = transactions.Select(t => new TransactionDto
                {
                    Id = t.Id ?? "Unknown",
                    UserId = t.UserId ?? "Unknown",
                    Email = t.Email ?? "Unknown",
                    PaymentGateway = t.PaymentGateway ?? "Unknown",
                    ExternalTransactionId = t.ExternalTransactionId ?? "Unknown",
                    Amount = t.Amount,
                    PlanType = t.PlanType ?? "Unknown",
                    Status = t.Status ?? "Unknown",
                    CreatedAt = t.CreatedAt,
                    CompletedAt = t.CompletedAt
                }).ToList();

                // Get summary statistics with null checks
                var gatewayStats = await _transactionRepository.GetGatewayStatsAsync(startDate, endDate);
                if (gatewayStats == null)
                {
                   
                    gatewayStats = new List<GatewayStatDto>(); // Use empty list instead of null
                }

                var planTypeStats = await _transactionRepository.GetPlanTypeStatsAsync(startDate, endDate);
                if (planTypeStats == null)
                {
                   
                    planTypeStats = new List<PlanTypeStatDto>(); // Use empty list instead of null
                }

                var totalAmount = await _transactionRepository.SumAmountAsync(predicate);

                var result = new TransactionReportDto
                {
                    Transactions = transactionDtos,
                    Summary = new TransactionSummaryDto
                    {
                        TotalTransactions = totalCount,
                        TotalAmount = totalAmount,
                        ByGateway = gatewayStats,
                        ByPlanType = planTypeStats
                    },
                    Pagination = new PaginationDto
                    {
                        CurrentPage = page,
                        PageSize = pageSize,
                        TotalItems = totalCount,
                        TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                    }
                };

                return Result<TransactionReportDto>.Success(result);
            }
            catch (Exception ex)
            {
        
                return Result<TransactionReportDto>.Failure($"Error generating report: {ex.Message}");
            }
        }
        public async Task<Result<AdminDashboardDto>> GetAdminDashboardDataAsync()
        {
            try
            {
                // Get data for the last 30 days
                var endDate = DateTime.UtcNow;
                var startDate = endDate.AddDays(-30);

                // Get total revenue for all transactions
                var totalRevenue = await _transactionRepository.SumAmountAsync(t => true);

                // Get total transactions count for all transactions
                var totalTransactions = await _transactionRepository.CountAsync(t => true);

                // Get subscriber counts
                var totalSubscribers = await _subscriptionRepository.CountAsync(s => true);
                var activeSubscribers = await _subscriptionRepository.CountAsync(s => s.ExpiryDate > DateTime.UtcNow);
                var expiredSubscribers = await _subscriptionRepository.CountAsync(s => s.ExpiryDate <= DateTime.UtcNow);

                // Get gateway breakdown
                var gatewayBreakdown = await _transactionRepository.GetGatewayStatsAsync(startDate, endDate);

                // Get plan type breakdown
                var planTypeBreakdown = await _transactionRepository.GetPlanTypeStatsAsync(startDate, endDate);

                // Optional: Get subscription plan breakdown
                var subscriptionPlanBreakdown = await GetSubscriptionPlanBreakdownAsync();

                var result = new AdminDashboardDto
                {
                    TotalRevenue = totalRevenue,
                    TotalTransactions = totalTransactions,
                    TotalSubscribers = totalSubscribers,
                    ActiveSubscribers = activeSubscribers,
                    ExpiredSubscribers = expiredSubscribers,
                    GatewayBreakdown = gatewayBreakdown,
                    PlanTypeBreakdown = planTypeBreakdown,
                    SubscriptionPlanBreakdown = subscriptionPlanBreakdown
                };

                return Result<AdminDashboardDto>.Success(result);
            }
            catch (Exception ex)
            {
                return Result<AdminDashboardDto>.Failure($"Error generating dashboard data: {ex.Message}");
            }
        }

        // Optional: Helper method to get subscription breakdown by plan
        private async Task<List<SubscriptionPlanStatDto>> GetSubscriptionPlanBreakdownAsync()
        {
            var now = DateTime.UtcNow;

            // Get all subscriptions grouped by plan type
            var subscriptions = await _subscriptionRepository.GetAllAsync();

            return subscriptions
                .GroupBy(s => s.PlanType)
                .Select(g => new SubscriptionPlanStatDto
                {
                    PlanType = g.Key,
                    TotalCount = g.Count(),
                    ActiveCount = g.Count(s => s.ExpiryDate > now),
                    ExpiredCount = g.Count(s => s.ExpiryDate <= now)
                })
                .ToList();
        }

        public async Task<Result<RevenueReportDto>> GetRevenueReportAsync(
            DateTime? startDate, DateTime? endDate, string groupBy, string status = null)
        {
            try
            {
                // Default to last 30 days if dates not provided
                var end = endDate ?? DateTime.UtcNow;
                var start = startDate ?? end.AddDays(-30);


                List<RevenueDataPointDto> data;

                // Get revenue data based on grouping
                switch (groupBy.ToLower())
                {
                    case "week":
                        var weeklyData = await _transactionRepository.GetWeeklyRevenueAsync(start, end);
                        data = weeklyData.Cast<RevenueDataPointDto>().ToList();
                        break;
                    case "month":
                        var monthlyData = await _transactionRepository.GetMonthlyRevenueAsync(start, end, status);
                        data = monthlyData.Cast<RevenueDataPointDto>().ToList();
                        break;
                    case "day":
                    default:
                        var dailyData = await _transactionRepository.GetDailyRevenueAsync(start, end);
                        data = dailyData.Cast<RevenueDataPointDto>().ToList();
                        break;
                }

                // Calculate total revenue
                var totalRevenue = data.Sum(d => d.Total);

                var result = new RevenueReportDto
                {
                    Data = data,
                    TotalRevenue = totalRevenue,
                    GroupBy = groupBy
                };

                return Result<RevenueReportDto>.Success(result);
            }
            catch (Exception ex)
            {
             
                return Result<RevenueReportDto>.Failure($"Error generating revenue report: {ex.Message}");
            }

        }
   


        public async Task<PaymentResponse> ProcessPaymentNotificationAsync(PaymentNotificationDto notification)
        {
            try
            {
                // Create a new PaymentRecord
                string modifiedCustomerRef = notification.CustomerRef.StartsWith("0")
                    ? "+234" + notification.CustomerRef.Substring(1)
                    : "+234" + notification.CustomerRef;
                var paymentRecord = new PaymentRecord
                {
                    PassBackReference = notification.PassBackReference,
                    TraceId = notification.TraceId,
                    PaymentReference = notification.PaymentReference,
                    CustomerRef = modifiedCustomerRef,
                    ResponseCode = notification.ResponseCode,
                    MerchantId = notification.MerchantId,
                    MobileNumber = notification.MobileNumber,
                    Amount = notification.Amount,
                    TransactionDate = notification.TransactionDate,
                    ShortCode = notification.ShortCode,
                    Currency = notification.Currency,
                    Channel = notification.Channel,
                    Hash = notification.Hash,
                };

                // Save the payment record
                await _paymentRepository.AddPaymentRecordAsync(paymentRecord);

                // If payment is successful, update the subscription
                if (notification.ResponseCode == "00")
                {
                    var subscriptionPlan = DeterminePlanType(notification.Amount);
                    await UpdateSubscriptionStatusWithCoralPay(modifiedCustomerRef, (SubscriptionPlan)Enum.Parse(typeof(SubscriptionPlan), subscriptionPlan.ToString()));
                    await UpdateUserSubscriptionStatus(modifiedCustomerRef, true);
                }

                string responseMessage = notification.ResponseCode == "00" ? "Payment Successful" : "Payment Failed";

                // Return the PaymentResponse using your existing class
                return new PaymentResponse
                {
                    ResponseCode = notification.ResponseCode,
                    ResponseMessage = responseMessage
                };
            }
            catch (Exception ex)
            {
                // In case of an exception, return a failed response
                return new PaymentResponse
                {
                    ResponseCode = "99", // Assuming "99" is your error code
                    ResponseMessage = $"An error occurred while processing the payment notification: {ex.Message}"
                };
            }
        }

        

        private async Task HandleSuccessfulTransaction(WebhookDataDto data)
        {

            await UpdateSubscriptionStatus(data);
            await SendPaymentConfirmationEmail(data);
        }
        private async Task UpdateSubscriptionStatus(WebhookDataDto data)
        {
            var user = await _userRepository.GetUserByEmailAsync(data.Customer.CustomerEmail);

            if (user != null)
            {
                var plan = DeterminePlanType(data.Amount);
                var subscriptionPlan = DeterminePlanType(data.Amount);
                var subscription = new Subscription
                {
                    UserId = user.Id,
                    StartDate = DateTime.UtcNow,
                    ExpiryDate = CalculateExpiryDate((SubscriptionPlan)Enum.Parse(typeof(SubscriptionPlan), subscriptionPlan.ToString())),
                    IsActive = true,
                    PlanType = plan.ToString(),
                    AmountPaid = data.Amount,
                    Currency = data.Currency ?? "NGN",
                    TransactionReference = data.TransRef,
                };
                user.HasActiveSubscription = true;
                await _subscriptionRepository.AddSubscriptionAsync(subscription);

                await _userRepository.UpdateUserAsync(user);
                await _userRepository.SaveChangesAsync();
            }
        }
        private async Task HandleFailedTransaction(WebhookDataDto data)
        {

            await SendPaymentFailureEmail(data);
        }



      

        private string GenerateUniqueReference()
        {
            return $"REF-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N").Substring(0, 8)}";
        }
          
       
        private async Task SendPaymentConfirmationEmail(WebhookDataDto data)
        {
            var emailSubject = "Payment Confirmation";
            var emailBody = $"Dear {data.Customer.FirstName},\n\nYour payment of {data.Amount} {data.Currency} has been successfully processed. " +
                            $"Transaction Reference: {data.TransRef}\n\nThank you for your subscription!";

            await _emailService.SendEmailAsync(data.Customer.CustomerEmail, emailSubject, emailBody);
        }

        private async Task SendPaymentFailureEmail(WebhookDataDto data)
        {
            var emailSubject = "Payment Failed";
            var emailBody = $"Dear {data.Customer.FirstName},\n\nWe regret to inform you that your payment of {data.Amount} {data.Currency} has failed. " +
                            $"Transaction Reference: {data.TransRef}\n\nPlease try again or contact our support team for assistance.";

            await _emailService.SendEmailAsync(data.Customer.CustomerEmail, emailSubject, emailBody);
        }

      
        public async Task<ServiceResult<PaymentVerificationResponseDto>> VerifyPaymentAsync(string transRef)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/transaction/{transRef}/verify");
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<PaymentVerificationResponseDto>>(responseContent);
                    if (result?.Status == 200)
                    {
                        if (result.Data.Status == 0) // Assuming 0 means successful payment
                        {
                            var user = await _userRepository.GetUserByEmailAsync(result.Data.CustomerId);
                            if (user != null)
                            {
                                user.HasActiveSubscription = true;
                                await _userRepository.UpdateUserAsync(user);
                            }
                        }
                        return new ServiceResult<PaymentVerificationResponseDto> { IsSuccess = true, Data = result.Data };
                    }
                    return new ServiceResult<PaymentVerificationResponseDto> { IsSuccess = false, ErrorMessage = result?.Message ?? "Failed to verify payment" };
                }

                return new ServiceResult<PaymentVerificationResponseDto> { IsSuccess = false, ErrorMessage = $"Failed to verify payment. Status code: {response.StatusCode}" };
            }
            catch (Exception ex)
            {
               
                return new ServiceResult<PaymentVerificationResponseDto> { IsSuccess = false, ErrorMessage = $"An error occurred while verifying payment: {ex.Message}" };
            }
        }

        public async Task<ServiceResult<DirectCardChargeResponseDto>> InitiateDirectCardChargeAsync(DirectCardChargeDto request)
        {
            try
            {
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/transaction/direct/initiate", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<DirectCardChargeResponseDto>>(responseContent);
                    if (result?.Status == 200)
                    {
                        return new ServiceResult<DirectCardChargeResponseDto> { IsSuccess = true, Data = result.Data };
                    }
                    return new ServiceResult<DirectCardChargeResponseDto> { IsSuccess = false, ErrorMessage = result?.Message ?? "Failed to initiate direct card charge" };
                }

                return new ServiceResult<DirectCardChargeResponseDto> { IsSuccess = false, ErrorMessage = $"Failed to initiate direct card charge. Status code: {response.StatusCode}" };
            }
            catch (Exception ex)
            {
               
                return new ServiceResult<DirectCardChargeResponseDto> { IsSuccess = false, ErrorMessage = $"An error occurred while initiating direct card charge: {ex.Message}" };
            }
        }

        public async Task<ServiceResult<AuthorizeCardChargeResponseDto>> AuthorizeDirectCardChargeAsync(AuthorizeCardChargeDto request)
        {
            try
            {
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/transaction/direct/authorize", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<AuthorizeCardChargeResponseDto>>(responseContent);
                    if (result?.Status == 200)
                    {
                        return new ServiceResult<AuthorizeCardChargeResponseDto> { IsSuccess = true, Data = result.Data };
                    }
                    return new ServiceResult<AuthorizeCardChargeResponseDto> { IsSuccess = false, ErrorMessage = result?.Message ?? "Failed to authorize card charge" };
                }

                return new ServiceResult<AuthorizeCardChargeResponseDto> { IsSuccess = false, ErrorMessage = $"Failed to authorize card charge. Status code: {response.StatusCode}" };
            }
            catch (Exception ex)
            {
              
                return new ServiceResult<AuthorizeCardChargeResponseDto> { IsSuccess = false, ErrorMessage = $"An error occurred while authorizing card charge: {ex.Message}" };
            }
        }

        public async Task<ServiceResult<BankAccountValidationResponseDto>> ValidateBankAccountAsync(BankAccountValidationDto request)
        {
            try
            {
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/settlement/bank/account/validate", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<BankAccountValidationResponseDto>>(responseContent);
                    if (result?.Status == 200)
                    {
                        return new ServiceResult<BankAccountValidationResponseDto> { IsSuccess = true, Data = result.Data };
                    }
                    return new ServiceResult<BankAccountValidationResponseDto> { IsSuccess = false, ErrorMessage = result?.Message ?? "Failed to validate bank account" };
                }

                return new ServiceResult<BankAccountValidationResponseDto> { IsSuccess = false, ErrorMessage = $"Failed to validate bank account. Status code: {response.StatusCode}" };
            }
            catch (Exception ex)
            {
               
                return new ServiceResult<BankAccountValidationResponseDto> { IsSuccess = false, ErrorMessage = $"An error occurred while validating bank account: {ex.Message}" };
            }
        }

        public async Task<ServiceResult<SettlementAccountResponseDto>> AddSettlementAccountAsync(SettlementAccountDto request)
        {
            try
            {
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/settlement/v2/accounts", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<SettlementAccountResponseDto>>(responseContent);
                    if (result?.Status == 200)
                    {
                        return new ServiceResult<SettlementAccountResponseDto> { IsSuccess = true, Data = result.Data };
                    }
                    return new ServiceResult<SettlementAccountResponseDto> { IsSuccess = false, ErrorMessage = result?.Message ?? "Failed to add settlement account" };
                }

                return new ServiceResult<SettlementAccountResponseDto> { IsSuccess = false, ErrorMessage = $"Failed to add settlement account. Status code: {response.StatusCode}" };
            }
            catch (Exception ex)
            {
           
                return new ServiceResult<SettlementAccountResponseDto> { IsSuccess = false, ErrorMessage = $"An error occurred while adding settlement account: {ex.Message}" };
            }
        }

        public class ApiResponse<T>
        {
            public int Status { get; set; }
            public string Message { get; set; }
            public T Data { get; set; }
            public double ExecTime { get; set; }
           
            public object Error { get; set; }
        }

        public async Task<NotificationResult<TransactionQueryResponseDto>> QueryTransactionDetailsAsync(string traceId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"TransactionQuery/{traceId}");

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var queryResponse = JsonSerializer.Deserialize<TransactionQueryResponseDto>(responseContent);

                    // TODO: Implement hash verification here
                    // var calculatedHash = CalculateHash(queryResponse);
                    // if (calculatedHash != queryResponse.Hash) {
                    //     return new ServiceResult<TransactionQueryResponseDto> { IsSuccess = false, ErrorMessage = "Hash verification failed." };
                    // }

                    return new NotificationResult<TransactionQueryResponseDto> { IsSuccess = true, Data = queryResponse };
                }
                else
                {
                    return new NotificationResult<TransactionQueryResponseDto> { IsSuccess = false, ResponseMessage = $"Error querying transaction. Status code: {response.StatusCode}" };
                }
            }
            catch (Exception ex)
            {
                return new NotificationResult<TransactionQueryResponseDto> { IsSuccess = false, ResponseMessage = $"An error occurred: {ex.Message}" };
            }
        }
        private async Task UpdateSubscriptionStatusWithCoralPay(string customerRef, SubscriptionPlan plan)
        {
            var user = await _userRepository.GetUserByCustomerRefAsync(customerRef);
            if (user == null)
            {
                throw new Exception($"User not found for customer reference: {customerRef}");
            }
            var existingSubscription = await _subscriptionRepository.GetSubscriptionByCustomerRefAsync(customerRef);
            if (existingSubscription != null)
            {
                existingSubscription.ExpiryDate = CalculateExpiryDate(plan);
                existingSubscription.PlanType = plan.ToString();
                await _subscriptionRepository.UpdateAsync(existingSubscription);
            }
            else
            {
                var newSubscription = new Subscription
                {
                    UserId = user.Id,
                    CustomerRef = customerRef,
                    StartDate = DateTime.UtcNow,
                    ExpiryDate = CalculateExpiryDate(plan),
                    IsActive = true,
                    PlanType = plan.ToString(),
                    AmountPaid = 0, // This should be updated with the actual amount
                    Currency = "NGN"
                };
                await _subscriptionRepository.AddAsync(newSubscription);
            }
        }
        
        public async Task<ServiceResult<PaymentStatusDto>> QueryTransactionAsync(string traceId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"transaction-query/{traceId}");

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var queryResponse = JsonSerializer.Deserialize<TransactionQueryResponseDto>(responseContent);

                    if (queryResponse.ResponseCode == "00")
                    {
                        var subscriptionPlan = DeterminePlanType(queryResponse.Amount);
                        await UpdateSubscriptionStatus(queryResponse.CustomerRef, subscriptionPlan);

                        var transactionQueryRecord = new TransactionQueryRecord
                        {
                            TraceId = traceId,
                            CustomerRef = queryResponse.CustomerRef,
                            Amount = queryResponse.Amount,
                            Currency = queryResponse.Currency,
                            TransactionDate = DateTime.UtcNow,
                            ResponseCode = queryResponse.ResponseCode,
                            ResponseMessage = queryResponse.ResponseMessage,
                            TransactionStatus = "Successful"
                        };
                        await _paymentRepository.AddTransactionQueryRecordAsync(transactionQueryRecord);

                        var paymentStatus = new PaymentStatusDto
                        {
                            HasActiveSubscription = true,
                            SubscriptionExpiryDate = CalculateExpiryDate((SubscriptionPlan)Enum.Parse(typeof(SubscriptionPlan), subscriptionPlan.ToString())),
                            TotalAmountPaid = queryResponse.Amount,
                            LastPaymentDate = queryResponse.TransactionDate,
                            LastPaymentStatus = "Successful"
                        };

                        return new ServiceResult<PaymentStatusDto> { IsSuccess = true, Data = paymentStatus };
                    }
                    else
                    {
                        return new ServiceResult<PaymentStatusDto> { IsSuccess = false, ErrorMessage = "Transaction query was not successful." };
                    }
                }
                else
                {
                    return new ServiceResult<PaymentStatusDto> { IsSuccess = false, ErrorMessage = "Error querying transaction." };
                }
            }
            catch (Exception ex)
            {
                return new ServiceResult<PaymentStatusDto> { IsSuccess = false, ErrorMessage = $"An error occurred: {ex.Message}" };
            }
        }
        private async Task UpdateUserSubscriptionStatus(string customerRef, bool hasActiveSubscription)
        {
            var user = await _userRepository.GetUserByPhoneNumberAsync(customerRef);
            if (user != null)
            {
                user.HasActiveSubscription = hasActiveSubscription;
                await _userRepository.UpdateUserAsync(user);
               
            }
            
        }
        public async Task<ServiceResult<bool>> VerifyEmailForSubscriptionAsync(string email)
        {
            var isVerified = await _verifiedEmailRepository.IsEmailVerifiedAsync(email);
            return new ServiceResult<bool> { IsSuccess = true, Data = isVerified };
        }

        public async Task<ServiceResult<bool>> ProcessPaymentWebhookAsync(PaymentWebhookDto webhookData)
        {
            try
            {
                if (!VerifyWebhookSignature(webhookData))
                {
                    return new ServiceResult<bool> { IsSuccess = false, ErrorMessage = "Invalid webhook signature." };
                }

                var paymentData = webhookData.Value.Data;
                var customerData = paymentData.Customer;
                // Retrieve the user by email
                var user = await _userRepository.GetUserByEmailAsync(customerData.Email);
                var planType = webhookData.Value.Data.PlanType;
                var email = webhookData.Value.Data.Customer.Email;



                var payment = new Payment
                {
                    UserId = user.Id,
                    Email = customerData.Email,
                    Amount = paymentData.Amount,
                    Status = paymentData.Status,
                    TransactionId = paymentData.Id,
                    PaymentDate = paymentData.CreatedAt

                   
                };

                await _paymentRepository.AddPaymentAsync(payment);
                if (string.IsNullOrWhiteSpace(planType))
                {
           
                    return new ServiceResult<bool> { IsSuccess = false, ErrorMessage = "Plan type is missing in the webhook data." };
                }
                await UpdateSubscriptionStatus(email, planType);

                if (string.Equals(paymentData.Status, "completed", StringComparison.OrdinalIgnoreCase))
                {
                    await UpdateSubscriptionStatus(customerData.Email, planType);
                    await SendPaymentConfirmationEmail(customerData.Email, paymentData.Amount, DateTime.UtcNow.AddMonths(1));
                }

                return new ServiceResult<bool> { IsSuccess = true, Data = true };
            }
            catch (Exception ex)
            {
            
                return new ServiceResult<bool> { IsSuccess = false, ErrorMessage = $"An error occurred while processing the payment webhook: {ex.Message}" };
            }
        }
       

        public async Task<ServiceResult<PaymentStatusDto>> GetPaymentStatusAsync(string userId)
        {
            try
            {
                var user = await _userRepository.GetUserByIdAsync(userId);
                if (user == null)
                {
                    return new ServiceResult<PaymentStatusDto> { IsSuccess = false, ErrorMessage = "User not found." };
                }

                var subscription = await _subscriptionRepository.GetActiveSubscriptionAsync(userId);
                var latestPayment = await _paymentRepository.GetLatestPaymentAsync(user.Email);

                var paymentStatus = new PaymentStatusDto
                {
                    HasActiveSubscription = subscription != null && subscription.IsActive,
                    SubscriptionExpiryDate = subscription?.ExpiryDate,
                    TotalAmountPaid = latestPayment?.Amount ?? 0,
                    LastPaymentDate = latestPayment?.PaymentDate,
                    LastPaymentStatus = latestPayment?.Status
                };

                return new ServiceResult<PaymentStatusDto> { IsSuccess = true, Data = paymentStatus };
            }
            catch (Exception ex)
            {
                // Log the exception
                return new ServiceResult<PaymentStatusDto> { IsSuccess = false, ErrorMessage = "An error occurred while fetching the payment status." };
            }
        }

        public async Task<ServiceResult<bool>> ProcessPaymentResponseAsync(PaymentResponseDto paymentResponse)
        {
            try
            {
                if (paymentResponse.Status)
                {

                    var payment = new Payment
                    {
                        Email = paymentResponse.Data.Customer.Email,
                        Amount = paymentResponse.Data.Amount,
                        Status = paymentResponse.Data.Status,
                        TransactionId = paymentResponse.Data.Id,
                        PaymentDate = paymentResponse.Data.CreatedAt,
                        //UserId = await GetUserIdByEmail(customerData.Email)

                    };

                    await _paymentRepository.AddPaymentAsync(payment);

                    if (paymentResponse.Data.Status.Equals("success", StringComparison.OrdinalIgnoreCase))
                    {
                        var planType = DeterminePlanType(paymentResponse.Data.Amount);
                        await UpdateSubscriptionStatus(payment.Email, planType);
                        await SendPaymentConfirmationEmail(payment.Email, payment.Amount, DateTime.UtcNow.AddMonths(1));
                 
                    }
                    else
                    {
                       
                    }

                    return new ServiceResult<bool> { IsSuccess = true, Data = true };
                }
                else
                {
             
                    return new ServiceResult<bool> { IsSuccess = false, ErrorMessage = "Payment was not successful." };
                }
            }
            catch (Exception ex)
            {
                
                return new ServiceResult<bool> { IsSuccess = false, ErrorMessage = "An error occurred while processing the payment response." };
            }
        }


        public async Task<ServiceResult<bool>> HasSuccessfulPaymentAsync(string email)
        {
            try
            {
                var hasSuccessfulPayment = await _paymentRepository.HasSuccessfulPaymentAsync(email);
                return new ServiceResult<bool> { IsSuccess = true, Data = hasSuccessfulPayment };
            }
            catch (Exception ex)
            {
                // Log the exception
                return new ServiceResult<bool> { IsSuccess = false, ErrorMessage = "An error occurred while checking for successful payments." };
            }
        }

        public async Task<ServiceResult<bool>> InitiatePaymentAsync(decimal amount, string email)
        {
            try
            {
                var paymentRequest = new
                {
                    amount = amount,
                    currency = "NGN",
                    email = email,
                    callback_url = _configuration["Alatpay:CallbackUrl"]
                };

                var content = new StringContent(JsonSerializer.Serialize(paymentRequest), Encoding.UTF8, "application/json");
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_configuration["Alatpay:SecretKey"]}");

                var response = await _httpClient.PostAsync("/api/v1/transaction/initialize", content);
                var responseString = await response.Content.ReadAsStringAsync();
                var paymentResponse = JsonSerializer.Deserialize<PaymentResponseDto>(responseString);

                if (paymentResponse.Status)
                {
                    // Store the payment details
                    await _paymentRepository.AddPaymentAsync(new Payment
                    {
                        Email = email,
                        Amount = amount,
                        Status = "Pending",
                        TransactionId = paymentResponse.Data.Id,
                        PaymentDate = DateTime.UtcNow
                    });

                    return new ServiceResult<bool> { IsSuccess = true, Data = true, Message = "Payment initiated successfully." };
                }
                else
                {
                    return new ServiceResult<bool> { IsSuccess = false, ErrorMessage = "Failed to initiate payment." };
                }
            }
            catch (Exception ex)
            {
                return new ServiceResult<bool> { IsSuccess = false, ErrorMessage = $"An error occurred while initiating the payment: {ex.Message}" };
            }
        }

        private string DeterminePlanType(decimal amount)
        {
            // This is a simple example. Adjust the logic based on your actual pricing structure.
            if (amount >= 2100) return "OneMonth";
            if (amount >= 650) return "OneWeek";
            return "OneDay";
        }
      

        private async Task SendPaymentConfirmationEmail(string email, decimal amount, DateTime expiryDate)
        {
            var nigerianCulture = new CultureInfo("en-NG");

            string subject = "Payment Confirmation";
            string body = $"Thank you for your payment of {amount.ToString("C", nigerianCulture)}. Your subscription will be active until {expiryDate:d}.";
            await _emailService.SendEmailAsync(email, subject, body);
        }

        private bool VerifyWebhookSignature(PaymentWebhookDto webhookData)
        {
            // Implement webhook signature verification based on Alatpay's documentation
            // This is a placeholder implementation
            return true;
        }
        private async Task UpdateSubscriptionwithCoralPayStatus(string customerRef, SubscriptionPlan plan)
        {
            var user = await _userRepository.GetUserByCustomerRefAsync(customerRef);
            if (user == null)
            {
                throw new Exception($"User not found for customer reference: {customerRef}");
            }

            var existingSubscription = await _subscriptionRepository.GetActiveSubscriptionAsync(user.Id);
            if (existingSubscription != null)
            {
                existingSubscription.ExpiryDate = CalculateExpiryDate(plan);
                existingSubscription.PlanType = plan.ToString();
                existingSubscription.IsActive = true;
                await _subscriptionRepository.UpdateAsync(existingSubscription);
            }
            else
            {
                var newSubscription = new Subscription
                {
                    UserId = user.Id,
                    StartDate = DateTime.UtcNow,
                    ExpiryDate = CalculateExpiryDate(plan),
                    IsActive = true,
                    PlanType = plan.ToString(),
                    AmountPaid = 0, // This should be updated with the actual amount
                    Currency = "NGN",
                    CustomerRef = customerRef
                };
                await _subscriptionRepository.AddAsync(newSubscription);
            }
        }


        //private async Task UpdateSubscriptionStatus(string email, string planType)
        //{
        //    var user = await _userRepository.GetUserByEmailAsync(email);
        //    if (user == null)
        //    {
        //        throw new Exception($"User not found for email: {email}");
        //    }

        //    var existingSubscription = await _subscriptionRepository.GetActiveSubscriptionAsync(user.Id);
        //    if (existingSubscription != null)
        //    {
        //        existingSubscription.ExpiryDate = CalculateExpiryDate(Enum.Parse<SubscriptionPlan>(planType));
        //        existingSubscription.PlanType = planType;
        //        await _subscriptionRepository.UpdateAsync(existingSubscription);

        //    }
        //    else
        //    {
        //        var newSubscription = new Subscription
        //        {

        //            UserId = user.Id,
        //            StartDate = DateTime.UtcNow,
        //            ExpiryDate = CalculateExpiryDate(Enum.Parse<SubscriptionPlan>(planType)),
        //            IsActive = true,
        //            PlanType = planType,
        //            AmountPaid = 0, // This should be updated with the actual amount
        //            Currency = "NGN",




        //        };
        //        await _subscriptionRepository.AddAsync(newSubscription);
        //    }
        //}
        private async Task UpdateSubscriptionStatus(string email, string planType, string paymentGateway = "Paystack")
        {
            var user = await _userRepository.GetUserByEmailAsync(email);
            if (user == null)
            {
                throw new Exception($"User not found for email: {email}");
            }

            var existingSubscription = await _subscriptionRepository.GetActiveSubscriptionAsync(user.Id);
            if (existingSubscription != null)
            {
                existingSubscription.ExpiryDate = CalculateExpiryDate(Enum.Parse<SubscriptionPlan>(planType));
                existingSubscription.PlanType = planType;
                existingSubscription.PaymentGateway = paymentGateway; // Add this line
                await _subscriptionRepository.UpdateAsync(existingSubscription);
            }
            else
            {
                var newSubscription = new Subscription
                {
                    UserId = user.Id,
                    StartDate = DateTime.UtcNow,
                    ExpiryDate = CalculateExpiryDate(Enum.Parse<SubscriptionPlan>(planType)),
                    IsActive = true,
                    PlanType = planType,
                    AmountPaid = 0, // This should be updated with the actual amount
                    Currency = "NGN",
                    PaymentGateway = paymentGateway // Add this line
                };
                await _subscriptionRepository.AddAsync(newSubscription);
            }
        }
        private async Task SendPaymentConfirmationEmail(string customerRef, decimal amount)
        {
            var user = await _subscriptionRepository.GetUserByCustomerRefAsync(customerRef);
            if (user != null)
            {
                string subject = "Payment Confirmation";
                string body = $"Dear {user.User.FullName},\n\nThank you for your payment of {amount:C}. Your subscription is now active.\n\nBest regards,\nYour Service Team";
                await _emailService.SendEmailAsync(user.Email, subject, body);
            }
        }

      

        private string CalculateHash(PaymentRequestFromCoralPayDto request)
        {
            var input = $"{request.PaymentReference}{request.CustomerRef}{request.ResponseCode}{request.MerchantId}{request.Amount}|{_configuration["CoralPay:SecretKey"]}";
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
                return BitConverter.ToString(bytes).Replace("-", "").ToLower();
            }
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
        private SubscriptionPlan ParseSubscriptionPlan(string planType)
        {
            if (Enum.TryParse<SubscriptionPlan>(planType, out var result))
            {
                return result;
            }
            throw new ArgumentException($"Invalid subscription plan type: {planType}");
        }


        private async Task<string> GetUserIdByEmail(string email)
        {
            var user = await _userRepository.GetUserByEmailAsync(email);
            if (user == null)
            {
                throw new Exception($"User not found for email: {email}");
            }
            return user.Id;
        }
    }
}

