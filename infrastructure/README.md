# TaxFiler Infrastructure as Code (Terraform)

Diese Infrastruktur verwendet Terraform um die Azure Web App über GitHub Actions bereitzustellen.

## Voraussetzungen

1. **Azure Service Principal erstellen**
   
   Führen Sie folgenden Befehl in der Azure CLI aus:
   ```bash
   az ad sp create-for-rbac --name "TaxFilerGitHubActions" \
     --role contributor \
     --scopes /subscriptions/25010af9-7f24-4aca-9123-f7054c0566f7/resourceGroups/TaxFiler_group \
     --sdk-auth
   ```

2. **GitHub Secrets einrichten**

   Fügen Sie folgende Secrets in Ihrem GitHub Repository hinzu (Settings > Secrets and variables > Actions):

   - `AZURE_CREDENTIALS`: Die komplette JSON-Ausgabe des obigen `az ad sp create-for-rbac` Befehls
   - `AZURE_SUBSCRIPTION_ID`: `25010af9-7f24-4aca-9123-f7054c0566f7`

## Struktur

```
infrastructure/
├── main.tf                    # Hauptkonfiguration mit Ressourcen
├── variables.tf               # Variablen Definitionen
├── outputs.tf                 # Output Werte
├── terraform.tfvars.example   # Beispiel für eigene Werte
└── .gitignore                 # Terraform-spezifische Dateien

.github/
└── workflows/
    └── deploy-infrastructure.yml  # GitHub Actions Workflow
```

## Deployment

### Automatisches Deployment über GitHub Actions

Bei jedem Push auf den `main` Branch wird automatisch:
1. Terraform initialisiert und validiert
2. Ein Terraform Plan erstellt
3. Die Infrastruktur bereitgestellt (Apply)
4. Die .NET Anwendung gebaut und deployed

### Manuelles Deployment

Sie können das Deployment auch manuell in GitHub triggern:
1. Gehen Sie zu "Actions" in Ihrem GitHub Repository
2. Wählen Sie "Deploy Infrastructure and Application"
3. Klicken Sie auf "Run workflow"

## Lokale Entwicklung

### Terraform lokal ausführen

1. **Azure Login**
   ```bash
   az login
   ```

2. **Terraform initialisieren**
   ```bash
   cd infrastructure
   terraform init
   ```

3. **Terraform Plan erstellen**
   ```bash
   terraform plan
   ```

4. **Infrastruktur bereitstellen**
   ```bash
   terraform apply
   ```

5. **Infrastruktur zerstören** (falls nötig)
   ```bash
   terraform destroy
   ```

### Eigene Werte verwenden

Kopieren Sie die Beispieldatei und passen Sie die Werte an:
```bash
cd infrastructure
cp terraform.tfvars.example terraform.tfvars
# Bearbeiten Sie terraform.tfvars nach Bedarf
```

## Infrastruktur anpassen

### Web App Namen ändern

Der Name der Web-Anwendung ist in `variables.tf` definiert:

```hcl
variable "web_app_name" {
  description = "Name of the web app"
  type        = string
  default     = "TaxFiler"
}
```

Um den Namen zu ändern, haben Sie 3 Optionen:

**Option 1: In terraform.tfvars (Empfohlen)**
```hcl
web_app_name = "MeinNeuerWebAppName"
```

**Option 2: Direkt in variables.tf**
```hcl
variable "web_app_name" {
  default     = "MeinNeuerWebAppName"
}
```

**Option 3: Als Command-Line Parameter**
```bash
terraform apply -var="web_app_name=MeinNeuerWebAppName"
```

**Wichtig**: Der Web App Name muss in Azure eindeutig sein, da er Teil der URL wird: `https://{web_app_name}.azurewebsites.net`

### App Service Plan SKU ändern

In `variables.tf` oder `terraform.tfvars`:
```hcl
app_service_plan_sku = "S1"  # von B1 zu S1
```

Verfügbare SKUs: F1, B1, B2, B3, S1, S2, S3, P1v2, P2v2, P3v2

### Tags hinzufügen

In `terraform.tfvars`:
```hcl
tags = {
  Environment = "Production"
  Project     = "TaxFiler"
  ManagedBy   = "Terraform"
}
```

### App Settings hinzufügen

In `main.tf` im `azurerm_linux_web_app` Resource:
```hcl
app_settings = {
  "APPLICATIONINSIGHTS_CONNECTION_STRING" = data.azurerm_application_insights.taxfiler.connection_string
  "MY_CUSTOM_SETTING" = "value"
  # Weitere Settings hier...
}
```

## Terraform State

Standardmäßig wird der Terraform State lokal gespeichert. Für Production empfiehlt sich ein Remote Backend.

### Azure Storage Backend einrichten

1. **Storage Account erstellen**
   ```bash
   az storage account create \
     --name taxfilerterraformstate \
     --resource-group TaxFiler_group \
     --location germanywestcentral \
     --sku Standard_LRS

   az storage container create \
     --name tfstate \
     --account-name taxfilerterraformstate
   ```

2. **Backend in main.tf aktivieren** (bereits vorbereitet, Kommentare entfernen)
   ```hcl
   backend "azurerm" {
     resource_group_name  = "TaxFiler_group"
     storage_account_name = "taxfilerterraformstate"
     container_name       = "tfstate"
     key                  = "terraform.tfstate"
   }
   ```

## Terraform Befehle

| Befehl | Beschreibung |
|--------|--------------|
| `terraform init` | Initialisiert Terraform und lädt Provider |
| `terraform fmt` | Formatiert Terraform Dateien |
| `terraform validate` | Validiert die Konfiguration |
| `terraform plan` | Zeigt geplante Änderungen an |
| `terraform apply` | Wendet Änderungen an |
| `terraform destroy` | Zerstört die Infrastruktur |
| `terraform output` | Zeigt Output Werte an |
| `terraform state list` | Listet alle Ressourcen im State |

## Wichtige Hinweise

- Die User Assigned Managed Identity muss bereits existieren
- Application Insights muss bereits existieren
- Die Resource Group muss bereits existieren
- Der Terraform State sollte in Production remote gespeichert werden
- Verwenden Sie `.gitignore` um sensitive Dateien auszuschließen

## Weitere Ressourcen hinzufügen

Um weitere Azure Ressourcen hinzuzufügen (z.B. Storage Account, Key Vault), erweitern Sie `main.tf`:

```hcl
resource "azurerm_storage_account" "example" {
  name                     = "taxfilerstorage"
  resource_group_name      = var.resource_group_name
  location                 = var.location
  account_tier             = "Standard"
  account_replication_type = "LRS"
}
```

## Troubleshooting

**Problem**: Terraform kann Ressourcen nicht finden
- Überprüfen Sie, ob die Managed Identity und App Insights existieren
- Stellen Sie sicher, dass Sie die richtigen Permissions haben

**Problem**: State Lock Fehler
- Verwenden Sie `terraform force-unlock <LOCK_ID>` nur wenn sicher

**Problem**: Änderungen werden nicht angewendet
- Prüfen Sie mit `terraform plan` welche Änderungen geplant sind
- Verwenden Sie `terraform apply -refresh=true`
