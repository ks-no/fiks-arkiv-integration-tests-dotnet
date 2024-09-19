# fiks-arkiv-integration-tests-dotnet
Integrasjonstester for Fiks-Arkiv protokollen. 
Disse testene er stort sett mer avanserte enn de testene man kan kjøre i [Fiks-Protokoll-Validator](https://forvaltning.fiks.test.ks.no/fiks-validator/#/).
Det vil si at integrasjonstestene skal f.eks. bevise at man kan først arkivere en journalpost for så å oppdatere den og til slutt hente den og bevise at endringen har blitt gjennomført samt mottat- og kvitteringsmeldinger ser korrekt ut. 

Fiks-Protokoll-Validator tester enkeltvis meldinger som f.eks. at man kan arkivere en journalpost og at kvitteringsmelding ser korrekt ut. Stegvise meldingsutvekslinger skal skrives som integrasjonstester.  

## Oppsett
For å kunne kjøre testene må man ha satt opp en Fiks-Protokoll konto som igjen er en FIks-IO konto. Se gjerne dokumentasjon [her](https://ks-no.github.io/fiks-plattform/tjenester/fiksio/) for nærmere forklaring rundt Fiks-Protokoller, Fiks-Arkiv, Fiks-IO og hvordan det henger sammen.

Husk også å sørge for at kontoen som kjører integrasjonstesten har satt opp å kunne sende og motta meldinger fra "Arkiv-kontoen".
Altså at de to kontoene har godkjent å kommunisere sammen via Fiks Protokoll. 

### Config

I prosjektet ligger det en appsettings.json. Kopier denne og gi den nye filen navnet **appsettings.Local.json**
Den nye filen skal man da putte inn sine konfigurasjonsdetaljer for Fiks-Protokoller/Fiks-IO.  

```json

{
  "TestConfig": {
    "ArkivAccountId" : "RECEIVING_ACCOUNT_GUID_IN_FIKSIO_HERE",
    "FagsystemName" : "Standard fagsystem-navn i ditt system",
    "SaksbehandlerName": "Standard saksbehandler-navn i ditt system",
    "KlassifikasjonKlasseID": "001",
    "KlassifikasjonssystemID": "EMNE"
  },
  "FiksIOConfig": {
    "ApiHost": "api.fiks.test.ks.no",
    "ApiPort": "443",
    "ApiScheme": "https",
    "AmqpHost": "io.fiks.test.ks.no",
    "AmqpPort": "5671",
    "FiksIoAccountId": "SENDERS_GUID_IN_FIKSIO_HERE",
    "FiksIoIntegrationId": "GUID_FOR_INTEGRATION_TO_FIKS_HERE",
    "FiksIoIntegrationPassword": "PASSWORD_FOR_INTEGRATION_TO_FIKS_HERE",
    "FiksIoIntegrationScope": "ks:fiks",
    "FiksIoPrivateKey": "PATH\\TO\\PRIVKEY.PEM",
    "MaskinPortenAudienceUrl": "http://test.maskinporten.no/",
    "MaskinPortenCompanyCertificateThumbprint": "",
    "MaskinPortenCompanyCertificatePath": "PATH\\TO\\MASKINPORTENCERT.p12",
    "MaskinPortenCompanyCertificatePassword": "PASSWORD_FOR_MASKINPORTENCERT",
    "MaskinPortenIssuer": "dummyIssuer",
    "MaskinPortenTokenUrl": "https://oidc-ver2.difi.no/idporten-oidc-provider/token",
    "AsiceSigningPrivateKey": "PATH\\TO\\SIGNING_PRIVATEKEY.PEM",
    "AsiceSigningPublicKey": "PATH\\TO\\SIGNING_PUBLICKEY.PEM"
  }
}
```

#### Forklaring
- `TestConfig:ArkivAccountId`: Fiks Protokoll kontoen for arkivet som skal motta meldingene.
- `TestConfig:FagsystemName`: Verdien her brukes som standard i `system` i arkivmelding i testene
- `TestConfig:SaksbehandlerName`: Verdien her brukes som standard for `saksbehandler` i arkivmelding i testene

For å forenkle test-oppsett kan man la være å oppgi `AsiceSigningPublicKey` og
`AsiceSigningPrivateKey` hvor den da vil forsøke å benytte sertifikatet oppgitt
for MaskinPorten. Merk at dette ikke er anbefalt i produksjon.

#### Tilpasninger
Det kan være det må gjøres tilpasninger i verdier i testene for å få de til å kjøre mot et arkiv.

Det kan også være at arkivet må sette opp `regel` og/eller systemet ditt i sitt arkiv.


### Test oppsett
Hver test sender inn en unik id som header på Fiks-IO meldingen med navnet **testSessionId**. Dette er kun for at vår arkiv-simulator skal kunne se hvilke meldinger som hører sammen når man kjører disse testene mot simulatoren. Hvis man kjører disse testene mot en arkiv implementasjon kan arkivet ignorere denne id'en. Den er kun for intern validering av integrasjonstestene.

### Testene

Selve testene ligger under **Tests** mappen. De er gruppert på typer tester i undermappene for **Brukstilfeller** og **Meldingstyper**.

- **Brukstilfeller** mappen inneholder tester basert på eksempler fra brukstilfellene for Fiks Arkiv, som f.eks. **_Elevmappe_**
- **Meldingstyper** mappen inneholder tester basert på meldingstypene for å kunne arkivere, oppdatere, hente og søk.


#### Brukstilfeller - Elevmappe
Tester som arkiverer ut i fra brukstilfellet **_Elevmappe_**.

#### Meldingstyper - Arkivering
Tester som først oppretter en ressurs i arkivet for så å hente den igjen for å sjekke at opprettelsen har blitt gjennomført.

#### Meldingstyper - ArkivmeldingOppdatering
Tester som først oppretter en ressurs i arkivet for så å oppdatere den og så til slutt hente den igjen for å sjekke at oppdatering har blitt gjennomført. 

#### Meldingstyper - Feilmelding
Tester som framprovoserer feil og sjekker at man får feilmelding. 

#### Meldingstyper - Innsyn
Tester som først oppretter en ressurs i arkivet for å så å hente dem. 

#### Meldingstyper - Sok
Tester som først oppretter en ressurs i arkivet for å så å søke etter dem.

### Flere tester?
Vi jobber med å utvide med flere tester, men det er ingenting i veien for å skrive noen selv og komme med pull-requests på dette repoet. Jo flere tester vi får jo bedre blir dette :)


