# API Endpoint Examples - Multilingual Feature

## 📚 Complete Endpoint Reference

All examples use `http://localhost:5279` (adjust for your environment).

---

## 1️⃣ Chat with Auto-Language Detection

### English
```bash
curl -X POST http://localhost:5279/api/chat/ai \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN_HERE" \
  -d '{
    "message": "Which team should I bet on in the derby?"
  }'
```

### Igbo
```bash
curl -X POST http://localhost:5279/api/chat/ai \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN_HERE" \
  -d '{
    "message": "Kedu ndị ịnà-ezipụta m nke osikidike?"
  }'
```

### Hausa
```bash
curl -X POST http://localhost:5279/api/chat/ai \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN_HERE" \
  -d '{
    "message": "Wane koli shine mafi kyau na gida?"
  }'
```

### Yoruba
```bash
curl -X POST http://localhost:5279/api/chat/ai \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN_HERE" \
  -d '{
    "message": "Iru ẹgbẹ̀ wo ní dara jù lọ?"
  }'
```

### Pidgin English
```bash
curl -X POST http://localhost:5279/api/chat/ai \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN_HERE" \
  -d '{
    "message": "Wetin be the best play for this match, my guy?"
  }'
```

---

## 2️⃣ Chat with Explicit Language Override

Use when you want to force a specific language regardless of message content:

```bash
curl -X POST http://localhost:5279/api/chat/ai \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN_HERE" \
  -d '{
    "message": "Which team should I back?",
    "language": "ig"
  }'
```

**Response**: AI responds in Igbo even though the question is in English

---

## 3️⃣ Chat with All Optional Parameters

```bash
curl -X POST http://localhost:5279/api/chat/ai \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN_HERE" \
  -d '{
    "message": "Kedu ndị ịnà-ezipụta m?",
    "language": "ig",
    "tone": "confident",
    "scope": "premier-league",
    "context": "Derby day match",
    "returnVoice": false
  }'
```

---

## 4️⃣ Set User Language Preference

Sets the user's default language for all future conversations:

```bash
curl -X POST http://localhost:5279/api/chat/preferences/language \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN_HERE" \
  -d '{
    "language": "ig"
  }'
```

### Response Success (200)
```json
{
  "message": "Language preference updated",
  "language": "ig",
  "whatsAppConfigured": false
}
```

### Response Error - Invalid Language (400)
```json
{
  "message": "Invalid language. Supported: en, ig, ha, yo, pcm"
}
```

---

## 5️⃣ Set Language + WhatsApp Number

Enable WhatsApp notifications by providing phone number:

```bash
curl -X POST http://localhost:5279/api/chat/preferences/language \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN_HERE" \
  -d '{
    "language": "ig",
    "whatsAppPhoneNumber": "+2348012345678"
  }'
```

### Response (200)
```json
{
  "message": "Language preference updated",
  "language": "ig",
  "whatsAppConfigured": true
}
```

**Note**: Phone number must be in E.164 format: `+[country code][number]`

---

## 6️⃣ Get Supported Languages

Get list of all supported languages with flags:

```bash
curl -X GET http://localhost:5279/api/chat/languages \
  -H "Authorization: Bearer YOUR_TOKEN_HERE"
```

### Response (200)
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
    {
      "code": "ha",
      "name": "Hausa",
      "flag": "🇳🇬"
    },
    {
      "code": "yo",
      "name": "Yoruba",
      "flag": "🇳🇬"
    },
    {
      "code": "pcm",
      "name": "Pidgin English",
      "flag": "🇳🇬"
    }
  ]
}
```

---

## 📋 Complete Request/Response Examples

### Example 1: Igbo Chat with Full Flow

**Request**:
```bash
curl -X POST http://localhost:5279/api/chat/ai \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIs..." \
  -d '{
    "message": "Kedu ihe o na-ezo?",
    "tone": "curious",
    "scope": "football",
    "context": "Premier League"
  }'
```

**Response**:
```json
{
  "requiresSubscription": false,
  "message": "Ị na-ajụ ajụ mma! Manchester City na Liverpool nwere ike inwe ọnụ ọgụgụ dị mma... Mana jide ụgbọ gị, ndị agha nke ọzọ enwere ike igbukar ha.",
  "subscribeUrl": null,
  "voiceUrl": null,
  "agentDirective": null,
  "conversationsRemaining": 9
}
```

---

### Example 2: Language Preference + Notifications

**Step 1: Set Preference**
```bash
curl -X POST http://localhost:5279/api/chat/preferences/language \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIs..." \
  -d '{
    "language": "pcm",
    "whatsAppPhoneNumber": "+2348012345678"
  }'
```

**Response**:
```json
{
  "message": "Language preference updated",
  "language": "pcm",
  "whatsAppConfigured": true
}
```

**Step 2: Chat (now in Pidgin automatically)**
```bash
curl -X POST http://localhost:5279/api/chat/ai \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIs..." \
  -d '{
    "message": "Which team should I back?"
  }'
```

**Response** (in Pidgin):
```json
{
  "requiresSubscription": false,
  "message": "Abi na you dey ask the right question my guy! Man City looking sharp this season... their recent form dey mad. But Liverpool no be joke o, Salah go burst dem like groundnut if you no watch am well.",
  "conversationsRemaining": 9
}
```

---

## 🧪 Test Scenarios

### Test 1: Verify Auto-Detection Works
```bash
# Send in Yoruba
curl -X POST http://localhost:5279/api/chat/ai \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer TOKEN" \
  -d '{"message": "Iru ẹgbẹ̀ wo ní dara jù?"}'
```
Expected: Response in Yoruba ✅

---

### Test 2: Verify Language Override
```bash
# Send in English but request Hausa
curl -X POST http://localhost:5279/api/chat/ai \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer TOKEN" \
  -d '{
    "message": "Which team should I bet on?",
    "language": "ha"
  }'
```
Expected: Response in Hausa ✅

---

### Test 3: Verify Language Preference Sticks
```bash
# First, set preference
curl -X POST http://localhost:5279/api/chat/preferences/language \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer TOKEN" \
  -d '{"language": "yo"}'

# Then chat without specifying language
curl -X POST http://localhost:5279/api/chat/ai \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer TOKEN" \
  -d '{"message": "What is your prediction?"}'
```
Expected: Response in Yoruba ✅

---

### Test 4: Verify WhatsApp Configuration
```bash
curl -X POST http://localhost:5279/api/chat/preferences/language \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer TOKEN" \
  -d '{
    "language": "ig",
    "whatsAppPhoneNumber": "+2348012345678"
  }'
```
Expected: Response shows `"whatsAppConfigured": true` ✅

---

### Test 5: Verify Invalid Language Rejected
```bash
curl -X POST http://localhost:5279/api/chat/ai \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer TOKEN" \
  -d '{
    "message": "Test",
    "language": "xyz"
  }'
```
Expected: Auto-detected language used (falls back to detection) ✅

---

### Test 6: Get All Languages
```bash
curl -X GET http://localhost:5279/api/chat/languages \
  -H "Authorization: Bearer TOKEN"
```
Expected: Returns all 5 languages with names and flags ✅

---

## 🔑 Authorization

All endpoints except `/api/chat/languages` require **Bearer token**:

```bash
-H "Authorization: Bearer YOUR_JWT_TOKEN_HERE"
```

**How to get token**: 
- Login via `/api/auth/signin` 
- Use token from response

---

## ⚠️ Common Errors

### 401 Unauthorized
```json
{
  "message": "Authentication required"
}
```
**Fix**: Add valid JWT token in Authorization header

### 400 Bad Request - Invalid Language
```json
{
  "message": "Invalid language. Supported: en, ig, ha, yo, pcm"
}
```
**Fix**: Use one of the supported language codes

### 400 Bad Request - Invalid Phone
```json
{
  "message": "Phone must be in E.164 format"
}
```
**Fix**: Use format `+[country][number]`, e.g., `+2348012345678`

### 404 Not Found
```json
{
  "message": "User not found"
}
```
**Fix**: User ID doesn't exist, check authentication

### 500 Server Error
```json
{
  "message": "Failed to update preference",
  "error": "..."
}
```
**Fix**: Check server logs, ensure database is updated

---

## 📱 Example Frontend Integration

### React Example
```javascript
// Chat in auto-detected language
const response = await fetch('/api/chat/ai', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
    'Authorization': `Bearer ${token}`
  },
  body: JSON.stringify({
    message: "Kedu ihe a ma mụ?"
  })
});

// Set language preference
const prefResponse = await fetch('/api/chat/preferences/language', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
    'Authorization': `Bearer ${token}`
  },
  body: JSON.stringify({
    language: "ig",
    whatsAppPhoneNumber: "+2348012345678"
  })
});
```

---

## 🎯 Summary

- **Auto-detection**: Works automatically, no parameter needed
- **Override**: Pass `language` parameter to force specific language
- **Preference**: Set once, use for all future requests
- **Notifications**: Add WhatsApp number to receive alerts in your language
- **List Languages**: Use `/api/chat/languages` to show users their options

---

**Ready to test?** Copy any example above and replace `YOUR_TOKEN_HERE` with your actual JWT token! 🚀
