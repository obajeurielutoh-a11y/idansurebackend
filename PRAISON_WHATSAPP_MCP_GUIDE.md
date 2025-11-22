# PraisonAI Agent + WhatsApp MCP Integration Guide

## 🎯 Overview

This implementation integrates **PraisonAI Agent** with **WhatsApp Model Context Protocol (MCP)** to automatically send prediction updates to registered WhatsApp numbers. The system ensures every registered WhatsApp number gets timely updates in their preferred language.

## 🏗️ Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    IdanSure Backend API                     │
├─────────────────────────────────────────────────────────────┤
│                                                               │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  WhatsAppIntegrationController                      │   │
│  │  - Register WhatsApp numbers                        │   │
│  │  - Verify numbers                                   │   │
│  │  - Update preferences                               │   │
│  └─────────────┬───────────────────────────────────────┘   │
│                │                                              │
│  ┌─────────────▼───────────────────────────────────────┐   │
│  │  PraisonAIWhatsAppAgentService                      │   │
│  │  - Orchestrates message delivery via PraisonAI      │   │
│  │  - Handles multilingual templating                  │   │
│  │  - Manages retry logic & error handling             │   │
│  │  - Validates WhatsApp numbers                       │   │
│  └─────────────┬───────────────────────────────────────┘   │
│                │                                              │
│  ┌─────────────▼───────────────────────────────────────┐   │
│  │  WhatsAppMCPAdapter                                 │   │
│  │  - MCP Resource management                          │   │
│  │  - Protocol encoding/decoding                       │   │
│  │  - Broadcast coordination                           │   │
│  │  - Resource URIs (mcp://whatsapp/*)                 │   │
│  └─────────────┬───────────────────────────────────────┘   │
│                │                                              │
│  ┌─────────────▼───────────────────────────────────────┐   │
│  │  IWhatsAppProvider                                  │   │
│  │  (WhatsAppCloudProvider Implementation)             │   │
│  │  - WhatsApp Cloud API integration                   │   │
│  │  - Message formatting                               │   │
│  │  - Delivery tracking                                │   │
│  └─────────────┬───────────────────────────────────────┘   │
│                │                                              │
│                ▼                                              │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  WhatsApp Cloud API (Meta)                          │   │
│  │  - User message delivery                             │   │
│  │  - Delivery receipts                                 │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                               │
└─────────────────────────────────────────────────────────────┘
```

## 🔧 Key Components

### 1. **PraisonAIWhatsAppAgentService**
Orchestrates the entire WhatsApp notification workflow using PraisonAI Agent:

**Key Methods**:
- `SendPredictionUpdateViaAgentAsync()` - Main entry point for sending updates
- `VerifyWhatsAppNumberAsync()` - Validates WhatsApp number
- `NormalizeToE164()` - Converts numbers to international format
- `ValidateUserWhatsAppRegistration()` - Comprehensive validation

**Features**:
- Handles 4 Nigerian languages + English
- Groups users by language for efficient delivery
- Multilingual message templates
- Error tracking and logging

### 2. **WhatsAppMCPAdapter**
Implements Model Context Protocol integration:

**MCP Resources**:
```
mcp://whatsapp/recipient/{phone_number}  - Individual recipient
mcp://whatsapp/message/{message_id}      - Single message
mcp://whatsapp/broadcast/{broadcast_id}  - Broadcast campaign
```

**Classes**:
- `WhatsAppRecipientMCP` - Recipient resource
- `WhatsAppMessageMCP` - Message resource
- `WhatsAppBroadcastMCP` - Broadcast campaign

**Methods**:
- `CreateRecipientMCP()` - Create recipient resource
- `VerifyRecipientMCPAsync()` - Verify in MCP context
- `ExecuteBroadcastMCPAsync()` - Coordinate broadcast
- `QueueMessageMCPAsync()` - Queue message with retry logic

### 3. **WhatsAppIntegrationController**
REST API endpoints for WhatsApp management:

**Endpoints**:
- `POST /api/whatsappintegration/register` - Register new number
- `PUT /api/whatsappintegration/update` - Update existing number
- `POST /api/whatsappintegration/verify` - Verify without registration
- `POST /api/whatsappintegration/test-notification` - Send test
- `GET /api/whatsappintegration/mcp/{resourceUri}` - Get MCP resource
- `GET /api/whatsappintegration/status` - Check integration status

## 📋 Flow Diagram

### Registration & Verification Flow

```
User sends phone number
         │
         ▼
Controller validates format
         │
         ▼
PraisonAI Agent verifies via WhatsApp MCP
         │
         ├─ Valid? ─Yes─┐
         │               │
        No               ▼
         │          Create MCP Resource
         │               │
         │               ▼
         │          Verify in MCP Context
         │               │
         │               ├─ Success ─┐
         │               │           │
         │              Fail         ▼
         │               │      Store normalized number
         │               │           │
         ├─────────────┬──┘           ▼
         │             │          Return success
         ▼             ▼
       Fail          Success
       Return        Return 200
       Error         OK
```

### Prediction Update Broadcasting

```
Admin creates/updates prediction
         │
         ▼
Trigger PraisonAI Agent
         │
         ▼
Get eligible users (subscribed + WhatsApp enabled)
         │
         ▼
Group by language (en, ig, ha, yo, pcm)
         │
         ├─ English Users ──┐
         ├─ Igbo Users ─────┤
         ├─ Hausa Users ────├─ PraisonAI Agent processes
         ├─ Yoruba Users ───┤
         └─ Pidgin Users ───┘
         │
         ▼
For each language group:
  1. Build multilingual message
  2. Create MCP Broadcast resource
  3. Validate each recipient number
  4. Queue messages with retry logic
         │
         ▼
Execute broadcast via WhatsApp Cloud API
         │
         ▼
Return results (sent, failed, skipped)
```

## 🚀 Usage Examples

### 1. Register WhatsApp Number

```bash
curl -X POST http://localhost:5279/api/whatsappintegration/register \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{
    "phoneNumber": "08012345678",
    "preferredLanguage": "ig"
  }'
```

**Success Response (200)**:
```json
{
  "message": "WhatsApp number registered and verified successfully",
  "phoneNumber": "+2348012345678",
  "language": "ig",
  "mcpUri": "mcp://whatsapp/recipient/%2B2348012345678",
  "notificationsEnabled": true
}
```

**Error Response (400)**:
```json
{
  "message": "Invalid WhatsApp number",
  "errors": [
    "Phone number format is invalid. Use E.164 format: +234xxxxxxxxxx"
  ]
}
```

### 2. Verify WhatsApp Number

```bash
curl -X POST http://localhost:5279/api/whatsappintegration/verify \
  -H "Content-Type: application/json" \
  -d '{
    "phoneNumber": "08012345678"
  }'
```

**Response**:
```json
{
  "phoneNumber": "08012345678",
  "normalizedPhoneNumber": "+2348012345678",
  "isVerified": true,
  "message": "WhatsApp number is valid and ready to receive notifications"
}
```

### 3. Update WhatsApp Number

```bash
curl -X PUT http://localhost:5279/api/whatsappintegration/update \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{
    "phoneNumber": "+2349876543210",
    "preferredLanguage": "ha",
    "enableNotifications": true
  }'
```

### 4. Get Integration Status

```bash
curl -X GET http://localhost:5279/api/whatsappintegration/status
```

**Response**:
```json
{
  "message": "WhatsApp Integration Status",
  "status": "operational",
  "features": {
    "registration": "enabled",
    "verification": "enabled",
    "mcpAdapter": "active",
    "praisonAIIntegration": "enabled",
    "supportedLanguages": ["en", "ig", "ha", "yo", "pcm"]
  },
  "endpoints": {
    "register": "POST /api/whatsappintegration/register",
    "update": "PUT /api/whatsappintegration/update",
    "verify": "POST /api/whatsappintegration/verify",
    "testNotification": "POST /api/whatsappintegration/test-notification",
    "mcpResource": "GET /api/whatsappintegration/mcp/{resourceUri}"
  }
}
```

### 5. Send Prediction Update via PraisonAI Agent

```csharp
// In PredictionController or similar
var predictionService = serviceProvider.GetRequiredService<PraisonAIWhatsAppAgentService>();

var result = await predictionService.SendPredictionUpdateViaAgentAsync(
    prediction: prediction,
    predictionTitle: "Manchester United vs Liverpool",
    predictionDetails: "Manchester United looking strong. Back them for Win or Draw. 72% confidence."
);

// Result contains:
// - TotalRecipients: 1250
// - SuccessfulSends: 1200
// - FailedSends: 30
// - SkippedCount: 20
// - Errors: List of error messages
```

## 📝 Phone Number Formats Supported

The system automatically handles these formats:

| Input | Normalized | Status |
|-------|-----------|--------|
| `08012345678` | `+2348012345678` | ✅ |
| `+2348012345678` | `+2348012345678` | ✅ |
| `2348012345678` | `+2348012345678` | ✅ |
| `+234-801-234-5678` | `+2348012345678` | ✅ |
| `08012345` | (invalid) | ❌ |
| `invalid` | (invalid) | ❌ |

## 🌍 Language Support

Each language has localized message templates:

**English**:
```
🎯 IdanSure Prediction Update!
Manchester United vs Liverpool
View full predictions: https://www.idansure.com/predictions
Stay disciplined, bet wisely! 💪
```

**Igbo**:
```
🎯 IdanSure Ọrụ Mmara Mmalite!
Manchester United vs Liverpool
Lelee ihe zuru ezu: https://www.idansure.com/predictions
Nwei ike, zụọ ike! 💪
```

**Hausa**:
```
🎯 IdanSure Sabuwar Labari!
Manchester United vs Liverpool
Duba cikakke: https://www.idansure.com/predictions
Ka ci gaba da hikima! 💪
```

**Yoruba**:
```
🎯 IdanSure Ìfẹ́ Tuntun!
Manchester United vs Liverpool
Wò gbogbo àkọ́kọ́: https://www.idansure.com/predictions
Gbé ara e lójú! 💪
```

**Pidgin**:
```
🎯 IdanSure New Update!
Manchester United vs Liverpool
Check full gist: https://www.idansure.com/predictions
Keep sharp, bet correct! 💪
```

## ✅ Validation Rules

**Phone Number**:
- Must be 10-15 digits (E.164 format)
- Must start with +
- Must be valid WhatsApp number

**Language**:
- Supported: `en`, `ig`, `ha`, `yo`, `pcm`
- Default: `en`

**User Requirements** (for notifications):
- Must have active subscription
- Must have WhatsApp number registered
- Must have notifications enabled
- Must be verified via MCP

## 🔄 PraisonAI Agent Integration

### How It Works

1. **Agent Receives Task**: Controller calls `SendPredictionUpdateViaAgentAsync()`
2. **Agent Plans**: Determines eligible users and languages
3. **Agent Acts**: Queues messages and coordinates MCP resources
4. **Agent Observes**: Tracks delivery status and errors
5. **Agent Optimizes**: Implements retry logic for failed sends

### Agent Benefits

- **Parallel Processing**: Sends to multiple users simultaneously
- **Language Optimization**: Groups by language for efficiency
- **Intelligent Retry**: Exponential backoff for failed messages
- **Error Recovery**: Continues despite individual failures
- **Comprehensive Logging**: Full audit trail

## 🛡️ Error Handling

### Automatic Retries
- Failed sends: 3 retry attempts with exponential backoff
- Delay: 1s → 2s → 4s
- Skipped on: Invalid format, missing number

### Error Codes

| Error | Meaning | Solution |
|-------|---------|----------|
| `InvalidPhoneFormat` | Number not E.164 | Use +234xxxxxxxxxx format |
| `NotWhatsAppUser` | Number not on WhatsApp | Use different number |
| `VerificationFailed` | MCP verification failed | Re-register number |
| `DeliveryFailed` | Message failed to send | Check network, retry |

## 📊 Monitoring & Logging

All operations are logged with:
- User ID
- Phone number (normalized)
- Language selected
- Timestamp
- Success/failure status
- Error details

**Example Log**:
```
2025-11-21 15:32:45 INFO: Registering WhatsApp number for user user_123: 08012345678
2025-11-21 15:32:46 INFO: WhatsApp number verified successfully: +2348012345678
2025-11-21 15:32:47 INFO: WhatsApp number registered and verified for user user_123: +2348012345678
2025-11-21 15:33:00 INFO: Starting PraisonAI Agent task for prediction update: pred_456
2025-11-21 15:33:05 INFO: Processing 1200 users for language: ig
2025-11-21 15:33:45 INFO: PraisonAI Agent task completed. Successful: 1200, Failed: 30, Skipped: 20
```

## 🔐 Security Considerations

1. **Authentication**: All endpoints except `/status` require JWT token
2. **Validation**: Phone numbers validated before processing
3. **Rate Limiting**: Consider implementing per-user rate limits
4. **Privacy**: Store phone numbers securely
5. **GDPR**: Users can unregister/delete WhatsApp numbers

## 📱 Database Schema

```sql
-- Add to User table
ALTER TABLE Users ADD PreferredLanguage nvarchar(10) DEFAULT 'en';
ALTER TABLE Users ADD WhatsAppPhoneNumber nvarchar(20);
ALTER TABLE Users ADD WhatsAppVerifiedAt datetime2;
ALTER TABLE Users ADD WhatsAppVerified bit DEFAULT 0;
```

## 🚀 Deployment Checklist

- [ ] PraisonAIWhatsAppAgentService registered in DI
- [ ] WhatsAppMCPAdapter registered in DI
- [ ] WhatsAppIntegrationController added to routing
- [ ] Database migration applied
- [ ] WhatsApp credentials in .env
- [ ] Test phone number registered
- [ ] Test notification sent
- [ ] Logging configured
- [ ] Rate limiting configured
- [ ] Monitoring/alerting setup

## 🧪 Testing

### Manual Test Script
```csharp
// 1. Register number
var registerResult = await httpClient.PostAsync(
  "/api/whatsappintegration/register",
  new { phoneNumber = "08012345678", preferredLanguage = "ig" }
);

// 2. Verify registration
var verifyResult = await httpClient.PostAsync(
  "/api/whatsappintegration/verify",
  new { phoneNumber = "08012345678" }
);

// 3. Get status
var statusResult = await httpClient.GetAsync("/api/whatsappintegration/status");

// 4. Send test notification
var testResult = await httpClient.PostAsync(
  "/api/whatsappintegration/test-notification",
  null
);
```

## 📞 Troubleshooting

| Issue | Cause | Solution |
|-------|-------|----------|
| Verification fails | Wrong phone format | Use +234xxxxxxxxxx |
| Number not verified | Not WhatsApp user | Ensure WhatsApp installed |
| Messages not received | User disabled notifications | Check in WhatsApp settings |
| Rate limiting errors | Too many requests | Implement queue/throttling |
| MCP errors | Adapter misconfigured | Check configuration |

## 🎯 Success Metrics

Monitor these to ensure system health:

1. **Registration Success Rate**: Target > 95%
2. **Verification Time**: Target < 2 seconds
3. **Message Delivery Rate**: Target > 98%
4. **Broadcast Completion**: Target > 99%
5. **Error Rate**: Target < 1%

---

**Ready to integrate?** Follow the deployment checklist and test with the manual test script! 🚀
