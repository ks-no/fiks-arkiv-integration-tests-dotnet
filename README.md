# fiks-arkiv-integration-tests-dotnet
Integrasjonstester for Fiks-Arkiv protokollen. 
Disse testene er stort sett mer avanserte enn de testene man kan kjøre i [Fiks-Protokoll-Validator](https://forvaltning.fiks.test.ks.no/fiks-validator/#/).
Det vil si at integrasjonstestene skal f.eks. bevise at man kan først arkivere en journalpost for så å oppdatere den og til slutt hente den og bevise at endringen har blitt gjennomført samt mottat- og kvitteringsmeldinger ser korrekt ut. 

Fiks-Protokoll-Validator tester enkeltvis meldinger som f.eks. at man kan arkivere en journalpost og at kvitteringsmelding ser korrekt ut. Stegvise meldingsutvekslinger skal skrives som integrasjonstester.  

## Oppsett
For å kunne kjøre testene må man ha satt opp en Fiks-Protokoll konto som igjen er en FIks-IO konto. Se gjerne dokumentasjon [her](https://ks-no.github.io/fiks-plattform/tjenester/fiksio/) for nærmere forklaring rundt Fiks-Protokoller, Fiks-Arkiv, Fiks-IO og hvordan det henger sammen.

### Config

I prosjektet ligger det en appsettings.json. Kopier denne og gi den nye filen navnet **appsettings.Local.json**
Den nye filen skal man da putte inn sine konfigurasjonsdetaljer for Fiks-Protokoller/Fiks-IO.  

```json
{
  "TestConfig": {
    "ArkivAccountId" : "RECEIVING_ACCOUNT_GUID_IN_FIKSIO_HERE"
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
    "MaskinPortenAudienceUrl": "http://localhost:8080/oidc-provider-mock/",
    "MaskinPortenCompanyCertificateThumbprint": "",
    "MaskinPortenCompanyCertificatePath": "PATH\\TO\\MASKINPORTENCERT.p12",
    "MaskinPortenCompanyCertificatePassword": "PASSWORD_FOR_MASKINPORTENCERT",
    "MaskinPortenIssuer": "dummyIssuer",
    "MaskinPortenTokenUrl": "https://oidc-ver2.difi.no/idporten-oidc-provider/token"
  }
}
```

### Test oppsett
Hver test sender inn en unik id som header på Fiks-IO meldingen med navnet **testSessionId**. Dette er kun for at vår arkiv-simulator skal kunne se hvilke meldinger som hører sammen når man kjører disse testene mot simulatoren. Hvis man kjører disse testene mot en arkiv implementasjon kan arkivet ignorere denne id'en. Den er kun for intern validering av integrasjonstestene.

### Testene

Selve testene ligger under **Tests** mappen. De er gruppert på typer tester i mappene **ArkivmeldingOppdateringTests** og **InnsynTests**.

#### ArkivmeldingOppdateringTests
Tester som først oppretter en ressurs i arkivet for så å oppdatere den og så til slutt hente den igjen for å sjekke at oppdatering har blitt gjennomført. 

#### InnsynTests
Tester som først oppretter en ressurs i arkivet for å så å hente dem. 
Testing av søk vil komme her etter hvert. 

### Flere tester?
Vi jobber med å utvide med flere tester, men det er ingenting i veien for å skrive noen selv og komme med pull-requests på dette repoet. Jo flere tester vi får jo bedre blir dette :)


