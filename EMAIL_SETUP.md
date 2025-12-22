# E-post Setup (SMTP)

För att använda e-postfunktionaliteten behöver du konfigurera SMTP-inställningar.

## Rekommenderat: Brevo (tidigare Sendinblue)

Brevo är en professionell e-posttjänst med gratis tier (300 e-post/dag) som stödjer anpassad avsändaradress.

### Steg 1: Skapa Brevo-konto

1. Gå till https://www.brevo.com och skapa ett gratis konto
2. Verifiera din e-postadress
3. Logga in till Brevo Dashboard

### Steg 2: Skapa SMTP Key

1. I Brevo Dashboard, gå till **Settings** → **SMTP & API**
2. Under **SMTP**-sektionen, klicka på **Generate new SMTP key**
3. Ge nyckeln ett namn (t.ex. "SchedulerMVP")
4. **VIKTIGT**: Kopiera SMTP Key direkt - den visas bara en gång!
5. Notera även din **SMTP Login** (din e-postadress i Brevo)

### Steg 3: Verifiera avsändaradress (ENKEL variant - ingen DNS/domänverifiering!)

**VIKTIGT**: Du behöver INTE verifiera `sportadmin.se`-domänen (det kräver DNS-inställningar). Istället verifiera en e-postadress du har tillgång till:

1. Gå till **Settings** → **Senders & IP**
2. Klicka **Add a sender** (under "Senders" sektionen, INTE "Domains")
3. Ange en e-postadress du har tillgång till (t.ex. din Gmail, privat e-post, etc.)
4. Klicka på verifieringslänken som skickas till den e-postadressen
5. **Klart!** Ingen DNS-konfiguration behövs.

**OBS**: 
- Mottagarna kommer se: "Sportadmins Schemaläggning" <din-verifierade-email@example.com>
- Detta ser professionellt ut eftersom "From Name" är "Sportadmins Schemaläggning"
- För testversionen är detta helt okej och ser seriöst ut

### Steg 4: Konfigurera lokalt

Lägg till i `appsettings.Development.json`:

```json
{
  "Email": {
    "SmtpHost": "smtp-relay.brevo.com",
    "SmtpPort": "587",
    "SmtpUser": "din-brevo-email@example.com",
    "SmtpPassword": "din-smtp-key-från-brevo",
    "FromEmail": "din-brevo-email@example.com",
    "FromName": "Sportadmins Schemaläggning"
  }
}
```

**VIKTIGT**: 
- `SmtpUser` = Din Brevo e-postadress (samma som du registrerade kontot med)
- `SmtpPassword` = Din SMTP Key från Brevo
- `FromEmail` = Samma e-postadress som `SmtpUser` (din verifierade Brevo-adress)
- `FromName` = Detta är vad mottagarna ser som avsändarnamn - sätt till "Sportadmins Schemaläggning" för professionell känsla
- Lägg INTE `appsettings.Development.json` med lösenord i git! Lägg till den i `.gitignore`.

## Alternativ: Gmail

### Steg 1: Skapa App Password i Gmail

1. Gå till https://myaccount.google.com/security
2. Aktivera **2-Step Verification** om du inte redan har det
3. Gå till **App passwords** (https://myaccount.google.com/apppasswords)
4. Välj **Mail** och **Other (Custom name)**
5. Skriv t.ex. "SchedulerMVP"
6. Klicka **Generate**
7. **VIKTIGT**: Kopiera lösenordet (16 tecken) - det visas bara en gång!

### Steg 2: Konfigurera lokalt

Lägg till i `appsettings.Development.json`:

```json
{
  "Email": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": "587",
    "SmtpUser": "din-email@gmail.com",
    "SmtpPassword": "ditt-app-password-16-tecken",
    "FromEmail": "din-email@gmail.com",
    "FromName": "Sportadmins Schemaläggning"
  }
}
```

**VIKTIGT**: Lägg INTE `appsettings.Development.json` med lösenord i git! Lägg till den i `.gitignore`.

## Alternativ: Outlook.com (personlig)

```json
{
  "Email": {
    "SmtpHost": "smtp-mail.outlook.com",
    "SmtpPort": "587",
    "SmtpUser": "din-email@outlook.com",
    "SmtpPassword": "ditt-lösenord",
    "FromEmail": "din-email@outlook.com",
    "FromName": "Sportadmins Schemaläggning"
  }
}
```

## Alternativ: Egen SMTP-server

Om du har en egen SMTP-server (t.ex. via din domän):

```json
{
  "Email": {
    "SmtpHost": "smtp.din-domän.se",
    "SmtpPort": "587",
    "SmtpUser": "noreply@din-domän.se",
    "SmtpPassword": "ditt-lösenord",
    "FromEmail": "noreply@din-domän.se",
    "FromName": "Sportadmins Schemaläggning"
  }
}
```

## Azure App Service

1. Gå till din Azure App Service i Azure Portal
2. Gå till **Configuration** → **Application settings**
3. Lägg till följande inställningar:
   - `Email:SmtpHost` = `smtp-relay.brevo.com`
   - `Email:SmtpPort` = `587`
   - `Email:SmtpUser` = `din-brevo-email@example.com`
   - `Email:SmtpPassword` = `din-smtp-key-från-brevo`
   - `Email:FromEmail` = `henrik.kackur@sportadmin.se` (eller din verifierade Brevo-adress)
   - `Email:FromName` = `Sportadmins Schemaläggning`
4. Klicka **Save**

## Fly.io

```bash
fly secrets set EMAIL_SMTP_HOST="smtp-relay.brevo.com"
fly secrets set EMAIL_SMTP_PORT="587"
fly secrets set EMAIL_SMTP_USER="din-brevo-email@example.com"
fly secrets set EMAIL_SMTP_PASSWORD="din-smtp-key-från-brevo"
fly secrets set EMAIL_FROM_EMAIL="henrik.kackur@sportadmin.se"
fly secrets set EMAIL_FROM_NAME="Sportadmins Schemaläggning"
```

## Testa

Efter konfiguration, testa genom att:
1. Logga in som admin
2. Gå till Admin-sidan
3. Klicka "Skapa användare"
4. Ange en e-postadress
5. Klicka "Skicka inbjudan"
6. Kontrollera att e-posten kommer fram

## Felsökning

**Brevo "Authentication failed":**
- Kontrollera att du använder SMTP Key (inte API Key) som lösenord
- Kontrollera att `SmtpUser` är din Brevo e-postadress (samma som du registrerade kontot med)
- Kontrollera att `FromEmail` matchar en VERIFIERAD avsändare i Brevo (Settings → Senders & IP → se till att "Verified" är grönt)

**E-post kommer från en annan adress än sportadmin.se:**
- Detta är förväntat om du inte har verifierat domänen (kräver DNS-inställningar)
- Lösning: Använd en verifierad e-postadress du har tillgång till och sätt `FromName` till "Sportadmins Schemaläggning"
- Mottagarna ser professionell avsändare: "Sportadmins Schemaläggning" <din-email@example.com>
- För testversionen är detta helt okej och ser seriöst ut

**Gmail "Less secure app access" fel:**
- Använd App Password istället för vanligt lösenord
- Se till att 2-Step Verification är aktiverat

**"Authentication failed":**
- Kontrollera att användarnamn och lösenord är korrekta
- För Gmail: Använd App Password, inte vanligt lösenord
- För Microsoft 365: Använd App Password om MFA är aktiverat

**"Connection timeout":**
- Kontrollera att port 587 är öppen
- Vissa nätverk blockerar SMTP - testa från annat nätverk
- För Microsoft 365: Verifiera att SMTP är tillåtet från din IP/plats

**E-post kommer inte fram:**
- Kontrollera spam-mappen
- Kontrollera serverloggar för felmeddelanden
- Verifiera att SMTP-inställningarna är korrekta
