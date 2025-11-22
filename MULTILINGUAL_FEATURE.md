# Multilingual AI Chat & WhatsApp Notifications Feature

## Overview
This implementation adds support for 4 Nigerian languages (Igbo, Hausa, Yoruba, and Pidgin English) to the IdanSure AI chat system, with automatic WhatsApp notifications for prediction updates.

## Supported Languages

| Code | Name | Flag |
|------|------|------|
| `en` | English | 🇬🇧 |
| `ig` | Igbo | 🇳🇬 |
| `ha` | Hausa | 🇳🇬 |
| `yo` | Yoruba | 🇳🇬 |
| `pcm` | Pidgin English | 🇳🇬 |

## Features

### 1. Language Detection
- **Automatic Detection**: The system automatically detects the user's language from their message
- **Keyword-Based**: Uses language-specific keywords and character detection
- **User Override**: Users can explicitly specify their preferred language in requests

### 2. Multilingual Responses
- **Localized System Prompts**: Each language has a culturally appropriate system prompt for the AI
- **AI Response**: OpenAI generates responses in the user's preferred language
- **Natural Interaction**: Responses maintain the tone and style appropriate to each language

### 3. WhatsApp Notifications
- **Automatic Updates**: Subscribed users receive WhatsApp notifications when predictions are updated
- **Language Support**: Notifications are sent in the user's preferred language
- **Easy Configuration**: Users can set their WhatsApp number in their preferences

## API Endpoints

### 1. Chat with AI (Enhanced)
**Endpoint**: `POST /api/chat/ai`

**Request Body**:
```json
{
  "message": "Which team should I bet on?",
  "language": "ig",          // Optional: en, ig, ha, yo, pcm
  "tone": "neutral",         // Optional
  "scope": "football",       // Optional
  "context": "Premier League", // Optional
  "returnVoice": false       // Optional
}
```

**How Language Works**:
1. If `language` is provided, it's used
2. If not, the system auto-detects from the message
3. System prompt is localized to the detected/specified language
4. AI responds in that language

### 2. Set Language Preference
**Endpoint**: `POST /api/chat/preferences/language`

**Request Body**:
```json
{
  "language": "ig",
  "whatsAppPhoneNumber": "+2348012345678"  // Optional
}
```

**Response**:
```json
{
  "message": "Language preference updated",
  "language": "ig",
  "whatsAppConfigured": true
}
```

### 3. Get Supported Languages
**Endpoint**: `GET /api/chat/languages`

**Response**:
```json
{
  "message": "Supported languages",
  "totalCount": 5,
  "languages": [
    {
      "code": "en",
      "name": "English",
      "flag": "🇬🇧"
    },
    {
      "code": "ig",
      "name": "Igbo",
      "flag": "🇳🇬"
    },
    // ... more languages
  ]
}
```

## Database Changes

Two new columns were added to the `Users` table:

```sql
ALTER TABLE "Users" ADD COLUMN "PreferredLanguage" text DEFAULT 'en';
ALTER TABLE "Users" ADD COLUMN "WhatsAppPhoneNumber" text;
```

## Services

### LanguageDetectionService
**Location**: `SubscriptionSystem.Infrastructure/Services/LanguageDetectionService.cs`

**Key Methods**:
- `DetectLanguage(string message)` - Auto-detects language from text
- `ParseLanguageCode(string code)` - Converts language codes
- `GetLocalizedSystemPrompt(Language lang)` - Gets language-specific AI instructions

**Supported Detection**:
- Keyword matching (language-specific words)
- Character detection (diacritics)
- Pidgin-specific phrases

### PredictionNotificationService
**Location**: `SubscriptionSystem.Infrastructure/Services/PredictionNotificationService.cs`

**Key Methods**:
- `NotifySubscribedUsersAsync(string team1, string team2, ...)` - Sends multilingual WhatsApp notifications
- `SendTestNotificationAsync(string phoneNumber)` - Tests WhatsApp configuration

**Features**:
- Sends notifications in 5 languages
- Respects user language preferences
- Integrates with existing WhatsApp provider
- Safe error handling

## Configuration

### appsettings.json
```json
{
  "WhatsApp": {
    "Token": "your-whatsapp-token",
    "PhoneNumberId": "your-phone-number-id",
    "TestPhoneNumber": "+2348012345678"
  },
  "Ai": {
    "MaxFreeConversations": 10,
    "MaxResponseLength": 2000
  }
}
```

### .env File
```
OPENAI_API_KEY=sk-xxxx
WhatsApp__Token=your-token
WhatsApp__PhoneNumberId=your-id
WhatsApp__TestPhoneNumber=+234xxxxxxxxxx
```

## Usage Examples

### Example 1: Chat in Igbo
```bash
curl -X POST http://localhost:5279/api/chat/ai \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer {token}" \
  -d '{
    "message": "Kedu ndị ịnà-ezipụta m?"
  }'
```

**Response will be in Igbo**.

### Example 2: Chat with Explicit Language
```bash
curl -X POST http://localhost:5279/api/chat/ai \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer {token}" \
  -d '{
    "message": "Which team has the best chance?",
    "language": "pcm"
  }'
```

**Response will be in Pidgin English**.

### Example 3: Set User Language Preference
```bash
curl -X POST http://localhost:5279/api/chat/preferences/language \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer {token}" \
  -d '{
    "language": "ha",
    "whatsAppPhoneNumber": "+2348012345678"
  }'
```

## Localized System Prompts

Each language has a custom system prompt that:
- Explains the AI's role in the local language
- Provides culturally appropriate tone and style
- Maintains the betting assistant personality
- Respects local communication preferences

### English Example
> "You are IdanSure GPT - a friendly first-person football betting insight assistant..."

### Pidgin Example
> "I na IdanSure GPT - your boy wey dey help you make better soccer betting moves, no joke..."

### Igbo Example
> "Ị bụ IdanSure GPT - onye na-eme ihe nkwado mma nke soccer betting..."

## WhatsApp Notification Flow

1. **Admin Updates Prediction**: Prediction is created/updated in system
2. **Webhook Trigger**: Optional endpoint called to trigger notifications
3. **User Filtering**: System identifies subscribed users with WhatsApp enabled
4. **Language Selection**: Uses each user's preferred language
5. **Message Composition**: Builds multilingual notification
6. **WhatsApp Send**: Sends via WhatsApp Cloud API
7. **Logging**: Records delivery status

### Example Notification (English)
```
🎯 IdanSure Prediction Update!

Manchester United vs Liverpool
Date: 21/11/2025 15:30
Prediction: Manchester United Win
Confidence: 72%

View full details: https://www.idansure.com/predictions

Stay sharp! 💪
```

### Example Notification (Igbo)
```
🎯 IdanSure Ọrụ Mmara Mmalite!

Manchester United vs Liverpool
Oge: 21/11/2025 15:30
Mmara: Manchester United Win
Ntụkwasị obi: 72%

Lelee ihe zuru ezu: https://www.idansure.com/predictions

Nwei ike! 💪
```

## Future Enhancements

1. **More Languages**: Easily add support for more Nigerian/African languages
2. **Language Switching**: Allow mid-conversation language changes
3. **Bulk Notifications**: Optimize sending to thousands of users
4. **Translation API**: Use Azure Translator or Google Translate for more languages
5. **Language Verification**: Validate responses are in correct language
6. **Analytics**: Track language preferences and engagement by language
7. **User Feedback**: Rate response quality by language

## Migration

To apply these changes to your database:

```bash
cd SubscriptionSystem.Infrastructure
dotnet ef migrations add AddLanguageAndWhatsAppSupportToUser --startup-project ../SubscriptionSystem
dotnet ef database update --startup-project ../SubscriptionSystem
```

## Testing

### Test Language Detection
```csharp
var service = new LanguageDetectionService(logger);
var lang = service.DetectLanguage("Kedu ndị ịnà-ezipụta m?"); // Returns: Language.Igbo
```

### Test WhatsApp Notifications
```csharp
await predictionNotificationService.SendTestNotificationAsync("+2348012345678");
```

### Test Multilingual Responses
Make requests in different languages and verify AI responds appropriately.

## Troubleshooting

### Responses not in the correct language
1. Check user's `PreferredLanguage` is set correctly
2. Verify OpenAI API key is valid
3. Review system prompt for language code

### WhatsApp notifications not sending
1. Verify WhatsApp token and phone number ID in config
2. Check phone number is in E.164 format (+234xxxxxxxxxx)
3. Review logs for API errors
4. Test with SendTestNotificationAsync

### Language not detected
1. Ensure message contains language-specific keywords
2. Try specifying language explicitly in request
3. Check LanguageDetectionService keyword lists

## Contributing

To add more languages:

1. Add language to `Language` enum in `LanguageDetectionService`
2. Add keywords to `LanguageKeywords` dictionary
3. Create GetXxxSystemPrompt() method with localized instructions
4. Update `GetLocalizedSystemPrompt()` switch statement
5. Add language code support in `ParseLanguageCode()`
6. Test language detection and responses

## Support

For issues or feature requests, contact the development team.
