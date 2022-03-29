# fiks-arkiv-integration-tests-dotnet
Integrasjonstester for Fiks-Arkiv protokollen. 
Disse testene er stort sett mer avanserte enn de testen man kan kjøre i [Fiks-Protokoll-Validator](https://forvaltning.fiks.test.ks.no/fiks-validator/#/).
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
