# AZ-203 Vorbereitung (WIP)

Auf [https://www.microsoft.com/de-de/learning/exam-az-203.aspx](https://www.microsoft.com/de-de/learning/exam-az-203.aspx) gibt's die relevanten Informationen zur AZ-203 Prüfung, mit der man offiziell ein "Microsoft-zertifiziert: Azure Developer Associate" wird :-).

Ich habe nachfolgend die einzelnen Kapitel rausgeschrieben und die meiner Meinung nach wichtigen Details, Beispiele oder sonstige Informationen zur Vorbereitung auf die Prüfung dazugeschrieben.

Für die Verwaltung von Subscriptions, Ressourcegruppen oder Ressourcen kann mit Hilfe des Azure Portals, der Azure CLI, Powershell, SDKs oder direkt per REST gemacht werden. Aufgrund der Wiederverwendbarkeit ist das Azure Portal nur bedingt empfehlenswert.

## Allgemeines

### PowerShell
Zur Verwendung von PowerShell wird die aktuellste PowerShell Core (gibt's für Windows, Mac und Linux) empfohlen. Zusätzlich muss das Modul "Az" installiert werden.

```powershell
Install-Module -Name Az -AllowClobber -Scope CurrentUser
```

Anschließend kann ein Login gemacht werden.

```powershell
Login-AzAccount -SubscriptionName Test
```

Hier wird ein Link sowie ein Code angezeigt. Der Link wird im Browser geöffnet und dort der Code eingegeben.

### Azure CLI

Für Windows kann die Azure CLI unter [https://aka.ms/installazurecliwindows](https://aka.ms/installazurecliwindows) heruntergeladen und installiert werden.

Der Login wird wie folgt gemacht

```batch
az login
```

Die Befehle in der Azure CLI haben immer den selben Aufbau: az + Ressourcentyp + Verb. Beispiel:

```batch
az vm create ...
```

Der Nachteil, im Vergleich zu PowerShell ist, dass es keine automatische Vervollständigung und Darstellung der möglichen Parameter gibt. Diese müssen mittels der Hilfe zuerst herausgefunden werden.

```batch
az vm create --help
```

Daher bevorzuge ich PowerShell ;-).

### Ressourcengruppen

Jede Ressource, die in Azure erstellt wird, wird einer Ressourcengruppe zugeordnet. Dies hat u.a. den Vorteil, dass diese bei der Abrechnung speziell ausgewertet werden können und auch, dass eine Ressourcengruppe als gesamtes gelöscht werden kann, was beim Testen von einzelnen Ressourcen sehr vorteilhaft ist.

## Develop Azure Infrastructure as a Service compute solution

### Implement solutions that use virtual machines (VM)

#### provision VMs

Nachfolgend ein stark vereinfachtes Beispiel mit PowerShell. 

```powershell
New-AzVm `
  -ResourceGroupName TestRG `
  -Name VM `
  -Location "West Europe" `
  -OpenPorts 80,3389
```

Damit wird eine Windows Server 2016 mit der Standardgröße "Standard DS1 v2" erstellt. Zusätzlich wird ein NIC, ein virtuelles Netzwerk, eine öffentliche IP-Adresse, eine Netzwerk Sicherheitsgruppe sowie eine Festplatte erstellt. Im produktiven Umfeld würde man dies natürlich nicht so machen, sondern die einzelnen Elemente, die erstellt werden sollen, explizit angeben/benennen.

Anschließend kann die IP-Adresse abgefragt werden sowie mit RDM eine Remotesitzung gestartet werden.

```powershell
Get-AzPublicIpAddress | where Name -eq VM | select IpAddress
mstsc /v:11.11.11.11
```

Wichtige Randnotiz: wird eine VM heruntergefahren, wird diese trotzdem verrechnet! Um dies zu verhindern, muss die VM im Portal oder mittels Skript gestoppt (in den "deallocated"-Status) gebracht werden.

```powershell
Stop-AzVm ` 
  -ResourceGroupName TestRG `
  -Name VM
```

Aus bestehenden VMs können Images erstellt werden. Diese können zukünftig beim Erstellen neuer VMs verwendet werden. Bevor dies gemacht werden kann, muss das Betriebssystem hardwareunabhängig gemacht ("generalisiert") werden. Unter Windows wird hierfür "sysprep" und unter Linux "waagent" verwendet. Wichtig: Betriebssysteme, die generalisiert wurden, können nicht mehr verwendet werden!

Innerhalb der VM

```bash
%windir%\system32\sysprep\sysprep.exe /generalize /oobe /shutdown
```

Anschließend wird die VM stoppt, generalisiert, eine Image Config erstellt und das Image erstellt. Wichtig: Befehle erst ausführen, wenn die VM im Status "VM stopped" ist!

```powershell
Stop-AzVm `
  -ResourceGroupName TestRG `
  -Name VM

Set-AzVM `
  -ResourceGroupName TestRG `
  -Name VM `
  -Generalized

$vm = Get-AzVM `
  -ResourceGroupName TestRG `
  -Name VM

$image = New-AzImageConfig `
  -Location "West Europe" `
  -SourceVirtualMachineId $vm.ID

New-AzImage `
  -ResourceGroupName TestRG `
  -Image $image `
  -ImageName VMImage
```

Anschließend kann eine neue VM mit dem zuvor gespeicherten Image erstellt werden:

```powershell
New-AzVm `
  -ResourceGroupName TestRG `
  -Name VMNeu `
  -Location "West Europe" `
  -ImageName VMImage `
  -OpenPorts 80,3389
```

Mit folgendem Befehl kann der Status der VM abgefragt werden:

```powershell
Get-AzVm `
  -ResourceGroupName TestRG `
  -Name VM `
  -Status
```

#### create ARM templates

Bei einer ARM-Vorlage handelt es sich um eine JSON-Datei, die die zu erstellenden Ressourcen inkl. aller Parameter enthält. Die Parameter können dabei fix in der Datei stehen, können aber auch berechnet (über Variablen) oder aus einer separaten Parameter-Datei gelesen werden.

ARM-Vorlagen bieten u.a. folgende Vorteile:
* Deklarative Syntax
* Wiederholbar Ergebnisse
* Abbildung von Abhängigkeiten der Ressourcen
* Validierung der Vorlage

Folgende Eigenschaften sind pflicht in einer ARM-Vorlage:
* $schema - das zugrundeliegende Schema der Vorlagendatei
* contentVersion - die Version der Vorlage
* resources - die zu erstellenden Ressourcen

Eine solche Datei von Hand zu erstellen, wird vermutlich niemand machen. Stattdessen gibt es z.B. bei GitHub das Repository [https://github.com/Azure/azure-quickstart-templates](https://github.com/Azure/azure-quickstart-templates) mit vielen vorgefertigen Templates, die noch einfach an die eigenen Bedürfnisse angepasst werden können.

Nachfolgend das PowerShell-Skript, um das Template zu aktivieren:

```powershell
New-AzResourceGroupDeployment `
  -ResourceGroupName TestRG `
  -TemplateFile c:\temp\template.json `
  -TemplateParameterFile c:\temp\template.parameters.json
```

#### configure Azure Disk Encryption for VMs

Ziel von verschlüssleten Datenträgern ist, dass diese, falls sie gestohlen werden, nicht ausgelesen werden können. Für Windows-VMs wird hierfür BitLocker eingesetzt, für Linux-VMs dm-crypt.

Um Datenträger verschlüsseln zu können, muss ein KeyVault mit EnabledForDiskEncryption erstellt werden/vorhanden sein.

```powershell
New-AzKeyvault `
  -ResourceGroupName TestRG `
  -name TestKeyVault `
  -Location "West Europe" `
  -EnabledForDiskEncryption

$keyVault = Get-AzKeyVault `
  -ResourceGroupName TestRG `
  -VaultName TestKeyVault
```

Nachfolgend der Code zum Erstellen einer VM und anschließendem Verschlüsseln.

```powershell
New-AzVm `
  -ResourceGroupName TestRG `
  -Name VM `
  -Location "West Europe" `
  -OpenPorts 80,3389

Set-AzVMDiskEncryptionExtension `
  -ResourceGroupName TestRG `
  -VMName VM `
  -DiskEncryptionKeyVaultUrl $keyVault.VaultUri `
  -DiskEncryptionKeyVaultId $keyVault.ResourceId `
  -SkipVmBackup `
  -VolumeType All
```

### Implement batch jobs by using Azure Batch Services

#### manage batch jobs by using Batch Service API

#### run a batch job by using Azure CLI, Azure portal, and other tools

#### write code to run an Azure Batch Service job

### Create containerized solutions

#### create an Azure Managed Kubernetes Service (AKS) cluster

#### create container images for solutions

#### publish an image to Azure Container Registry

#### run containers by using Azure Container Instance or AKS

## Develop Azure Platform as a Service compute solutions

### Create Azure App Service Web Apps

#### create an Azure App Service Web App

#### create an Azure App Service background task by using WebJobs

#### enable diagnostics logging

#### create an Azure Web App for containers

#### monitor service health by using Azure Monitor

### Create Azure App Service mobile apps

#### add push notifications for mobile apps

#### enable offline sync for mobile app

#### implement a remote instrumentation strategy for mobile devices

### Create Azure Service API apps

#### create an Azure App Service API app

#### create documentation for the API by using open source and other tools

### Implement Azure functions

#### implement input and output bindings for a function

#### implement function triggers by using data operations, timers, and webhooks

#### implement Azure Durable Functions

#### create Azure Function apps by using Visual Studio

#### implement Python Azure functions

## Develop for Azure storage

### Develop solutions that use storage tables

#### design and implement policies for tables

#### query table storage by using code

#### implement partitioning schemes

### Develop solutions that use Cosmos DB storage

#### create, read, update, and delete data by using appropriate APIs

#### implement partitioning schemes

#### set the appropriate consistency level for operations

### Develop solutions that use relational database

#### provision and configure relational databases

#### configure elastic pools for Azure SQL Database

#### create, read, update, and delete tables by using code

#### provision and configure Azure SQL Database serverless instances

#### provision and configure Azure SQL and Azure PostgreSQL Hyperscale instaces

### Develop solutions that use blob storage

#### move items in Blob storage between storage accounts or container

#### set and retrieve properties and metadata

#### implement blob leasing

#### implement data archiving and retention

#### implement Geo Zone Redundant storage

## Implement Azure Security

### Implement authentication

#### implement authentication by using certificates, forms-based authentication, or tokens

#### implement multi-factor or Windows authentication by using Azure AD

#### implement OAuth2 authentication

#### implement Managend identities/Service Principal authentication

#### implement Microsoft identity platform

### Implement access control

#### implement CBAC (Claims-Based Access Control) authorization

#### implement RBAC (Role-Based Access Control) authorization

#### create shared access signatures

### Implement secure data solutions

#### encrypt and decrypt data at rest and in transit

#### create, read, update, and delete keys, secrets, and certificates by using the KeyVault API

## Monitor, troubleshoot, and optimize Azure Solutions

### Develop code to support scalability of apps and services

#### implement autoscaling rules and patterns (schedule, operational/system metrics, singleton applications)

#### implement code that handles transient faults

#### implement AKS scaling strategies

### Integrate caching and content delivery within solutions

#### store and retrieve data in Azure Redis cache

#### develop code to implement CDN's in solutions

#### invalidate cache content (CDN or Redis)

### Instrument solutions to support monitoring and logging

#### configure instrumentation in an app or service by using Applications Insights

#### analyze and troubleshoot solutions by using Azure Monitor

#### implement Applications Insights Web Test and Alerts

## Connect to and consume Azure services and third-party services

### Develop and App Service Logic App

#### create a Logic App

#### create a custom connector for Logic Apps

#### create a custom template for Logic Apps

### Integrate Azure Search within solutions

#### create an Azure Search index

#### import searchable data

#### query the Azure Search index

#### implement cognitive search

### Implement API management

#### establish API Gateways

#### create and APIM instance

#### configure authentication for APIs

#### define policies for APIs

### Develop event-based solutions

#### implement solutions that use Azure Event Grid

#### implement solutions that use Azure Notification Hubs

#### implement solutions that use Azure Event Hub

### Develop message-based solutions

#### implement solutions that use Azure Service Bus

#### implement solutions that use Azure Queue Storage queues