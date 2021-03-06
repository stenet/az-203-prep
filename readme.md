# AZ-203 Vorbereitung (WIP)

Auf [https://www.microsoft.com/de-de/learning/exam-az-203.aspx](https://www.microsoft.com/de-de/learning/exam-az-203.aspx) gibt es die relevanten Informationen zur AZ-203 Prüfung, mit der man offiziell ein "Microsoft-zertifiziert: Azure Developer Associate" wird :-).

Ich habe nachfolgend die einzelnen Kapitel rausgeschrieben und die meiner Meinung nach wichtigen Details, Beispiele oder sonstigen Informationen zur Vorbereitung auf die Prüfung ergänzt.

Die Verwaltung von Subscriptions, Resource groups oder Resources kann mit Hilfe des Azure Portals, der Azure CLI, Powershell, SDKs oder direkt per REST gemacht werden. Aufgrund der Wiederverwendbarkeit ist das Azure Portal nur bedingt empfehlenswert.

Unter [https://melcher.dev/2019/01/az-203-learning-material-/-link-collection/](https://melcher.dev/2019/01/az-203-learning-material-/-link-collection/) sind zu den einzelnen Kapiteln jeweils ein oder mehrere Links, die zu detaillierteren Informationen zum jeweiligen Thema führen.

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

Für Windows kann die Azure CLI unter [https://aka.ms/installazurecliwindows](https://aka.ms/installazurecliwindows) heruntergeladen und installiert werden. Diese steht aber auch für alle anderen Plattformen (Linux, Mac) zur Verfügung.

Der Login wird wie folgt gemacht:

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

Daher bevorzuge ich PowerShell, da ich dort mittels Strg + Leertaste oder Tab einen Vorschlag bekomme was möglich ist und nicht immer die Hilfe benötige. Nichts desto trotz habe ich festgestellt, dass manche Funktionalitäten in den PowerShell Cmdlets noch nicht zur Verfügung stehen bzw. nur sehr umständlich machbar sind. In diesem Fall bin ich auf die Azure CLI ausgewichen.

### Ressourcengruppen

Jede Ressource, die in Azure erstellt wird, wird einer Ressourcengruppe zugeordnet. Dies hat u.a. den Vorteil, dass diese bei der Abrechnung speziell ausgewertet werden können und auch, dass eine Ressourcengruppe als gesamtes gelöscht werden kann, was beim Testen von einzelnen Ressourcen sehr vorteilhaft ist.

### Links

* https://webhook.site - erstellt einen Endpunkt, was zum Beispiel beim Testen von OAuth2 weiter unten sehr hilfsreich war.

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

Damit wird eine Windows Server 2016 mit der Standardgröße "Standard DS1 v2" erstellt. Zusätzlich wird ein NIC, ein virtuelles Netzwerk, eine öffentliche IP-Adresse, eine Netzwerk Sicherheitsgruppe sowie eine Festplatte erstellt und die Ports 80 + 3389 geöffnet. Im produktiven Umfeld würde man dies natürlich nicht so machen, sondern die einzelnen Elemente, die erstellt werden sollen, explizit angeben/benennen.

Anschließend kann die IP-Adresse abgefragt sowie mit RDM eine Remotesitzung gestartet werden.

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

Innerhalb der VM:

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

Interessanter Zusatz: mit Invoke-AzVMRunCommand kann z.B. nach der Erstellung einer VM ein Skript innerhalb der VM ausgeführt werden. Alternativ gibt es die "Custom Script Extension", die Skripte auf die VM lädt (aus Azure Storage oder GitHub) und ausführt - was auch beim Post-Deployment interessant sein kann. 

#### create ARM templates

Bei einer ARM-Vorlage handelt es sich um eine JSON-Datei, die die zu erstellenden Ressourcen inkl. aller Parameter enthält. Die Parameter können dabei fix in der Datei stehen, können aber auch berechnet (über Variablen) oder aus einer separaten Parameter-Datei gelesen werden.

ARM-Vorlagen bieten u.a. folgende Vorteile:
* Deklarative Syntax
* Wiederholbare Ergebnisse
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

Ziel von verschlüsselten Datenträgern ist, dass diese, falls sie gestohlen werden, nicht ausgelesen werden können. Für Windows-VMs wird hierfür BitLocker eingesetzt, für Linux-VMs dm-crypt.

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

Um eine VM aus einer bestehenden VHD (Virtual-Hard-Disk) zu erstellen, kann der Befehl Add-AzVm verwendet werden. Die VHD kann (bei Windows) mit BitLocker verschlüsselt sein (ohen TPM - Trusted Platform Module).

### Implement batch jobs by using Azure Batch Services

Die nachfolgende Grafik zeigt den Ablauf bei Verwendung von Azure Batch.

![Überblick Azure Batch](images/az_batch_overview.png)

[https://docs.microsoft.com/en-us/azure/batch/batch-technical-overview](https://docs.microsoft.com/en-us/azure/batch/batch-technical-overview)

Mit Hilfe von Azure Batch können aufwändige Operationen, die sich parallel abarbeiten lassen, optimal und schnell durchgeführt werden. Im Batch Account werden Pools definiert. Der Pool definiert welches Betriebssystem verwendet werden soll, wie groß die Nodes sein sollen, wie sich die Skalierung verhalten soll, wie viele Nodes es geben soll, wie viele Tasks parallel auf einer Node ausgeführt werden können, die zu installierende Applikation, Netzwerk, usw.

#### manage batch jobs by using Batch Service API

Wenn der Pool definiert ist können Jobs erstellt werden. Ein Job kann wiederum mehrere Tasks haben. Der Task enthält Input-Dateien sowie den Befehl (Commandline), der zum Start ausgeführt werden soll.

Nachfolgend der Ablauf (C#) mit den wichtigsten Funktionen.

Client erstellen:

```csharp
var client = BatchClient.Open(credentials)
```

Pool erstellen:

```csharp
var pool = client.PoolOperations.CreatePool(
  POOL_ID,
  "Standard_A1_v2",
  new VirtualMachineConfiguration(
    imageReference: new ImageReference(
      publisher: "MicrosoftWindowsServer",
      offer: "WindowsServer",
      sku: "2016-datacenter-smalldisk",
      version: "latest"),
    nodeAgentSkuId: "batch.node.windows amd64"),
  targetDedicatedComputeNodes: 2);

pool.Commit();
```

Job erstellen:

```csharp
var job = client.JobOperations.CreateJob();
job.Id = JOB_ID;
job.PoolInformation = new PoolInformation() { PoolId = pool.Id };

job.Commit();
```

Task erstellen:

```csharp
var task = new CloudTask(id, commandline);
task.ResourceFiles = new List<ResourceFile>() { resourceFile };
```

Die Tasks werden in einer Liste gesammelt und gesamt zur Verarbeitung übergeben:

```csharp
client.JobOperations.AddTask(job.Id, taskList);
```

Prüfung, ob Tasks fertig sind:

```csharp
var addedTasks = client.JobOperations.ListTasks(job.Id);
var timeout = TimeSpan.FromMinutes(30);

client.Utilities.CreateTaskStateMonitor().WaitAll(addedTasks, TaskState.Completed, timeout);
```

Ein Beispiel hierfür ist unter [https://github.com/stenet/az-203-prep/tree/master/vs/AzBatch](https://github.com/stenet/az-203-prep/tree/master/vs/AzBatch).

Sollen Dateien nach der Verarbeitung eines Tasks gespeichert werden, so bei der Erstellung des CloudTasks die OutputFiles-Eigenschaft befüllt werden. Mit dieser kann eine einzelne Datei oder ein Pattern (*.txt) angegeben werden, ein Blob-Container, ... sowie unter welchen Umständen die Datei hochgeladen werden soll:

* Success - ExitCode = 0
* Failure - ExitCode != 0
* Completion - immer

#### run a batch job by using Azure CLI, Azure portal, and other tools

Das Beispiel von oben lässt sich mehr oder weniger auch in PowerShell nachbauen. Nachfolgend der Code dazu (inkl. dem Erzeugen eines Batch-Accounts, dafür ohne Storage):

```powershell
$batch = New-AzBatchAccount `
  -ResourceGroupName TestRG `
  -AccountName testbatch20200128 `
  -Location "West Europe" 

$imageRef = New-Object `
  -TypeName "Microsoft.Azure.Commands.Batch.Models.PSImageReference" `
  -ArgumentList @("windowsserver","microsoftwindowsserver","2019-datacenter-core")

$configuration = New-Object `
  -TypeName "Microsoft.Azure.Commands.Batch.Models.PSVirtualMachineConfiguration" `
  -ArgumentList @($imageRef, "batch.node.windows amd64")

New-AzBatchPool `
  -Id testpool `
  -VirtualMachineSize "Standard_a1" `
  -VirtualMachineConfiguration $configuration `
  -AutoScaleFormula '$TargetDedicated=2;' `
  -BatchContext $batch

$poolInfo = New-Object `
  -TypeName "Microsoft.Azure.Commands.Batch.Models.PSPoolInformation"

$poolInfo.PoolId = "testpool" 

$job = New-AzBatchJob `
  -PoolInformation $poolInfo `
  -Id testjob `
  -BatchContext $batch

$task01 = New-Object `
  -TypeName Microsoft.Azure.Commands.Batch.Models.PSCloudTask `
  -ArgumentList @("task01", "cmd /c dir /s")

$task = New-AzBatchTask `
  -JobId testjob `
  -BatchContext $batch `
  -Tasks @($task01)
```

#### write code to run an Azure Batch Service job

Dies wurde bereits zuvor behandelt ;-)

### Create containerized solutions

#### create an Azure Managed Kubernetes Service (AKS) cluster

Einführend ein paar Worte zu Kubernetes (K8s). Dies wurde 2014 von Google als Open-Source-Projekt veröffentlicht. Sinn von K8s ist die Orchestrierung und Überwachung von Containern. 

![Überblick K8s](images/az_aks_k8s.png)

[https://de.wikipedia.org/wiki/Kubernetes](https://de.wikipedia.org/wiki/Kubernetes)

Wie man in der Grafik oben sieht gibt es einen Master (könnten auch mehrere sein, ungerade Zahl) und mehrere Nodes. Der Master steuert den Cluster. Innerhalb einer Node arbeiten Pods (Arbeitsprozesse). Innerhalb eines Pods laufen ein oder mehrere Container.

Erstmal die Kubernetes CLI installieren. Klappt, wieso auch immer, am einfachsten über die Azure CLI ;-).

```bash
az aks install-cli
```

Anschließend wird angezeigt, dass der Ort, an dem die CLI installiert wurde, in den Path übernommen werden soll. Dies kann man wunderbar einfach mit PowerShell machen.

```powershell
$env:Path += "DER_PFAD_VON_OBEN"
```

Dann kann der Kubernetes Cluster erzeugt werden. Falls noch kein ssh-key erstellt wurde, muss dies zuerst gemacht werden.

```powershell
ssh-keygen
```

Jetzt den Cluster erstellen:

```powershell
New-AzAks `
  -ResourceGroupName TestRG `
  -Name TestAKS `
  -NodeCount 2 `
  -KubernetesVersion "1.14.8"
```

Anschließend muss mit der Azure CLI der Kontext für kubectl gesetzt werden. Die zwei folgenden Befehle haben bei mir in der PowerShell nicht zum gewünschten Ergebnis geführt. kubectl hat immer eine Fehlermeldung ausgegeben. Habe den Code dann in der Windows Commandline ausgeführt und dann hat's geklappt ...

```bash
az aks get-credentials --resource-group TestRG --name TestAKS
```

Um den Status der Node zu überprüfen:

```bash
kubectl get nodes
```

Der Cluster steht und die Container können können aktiviert werden. Dafür wird eine yaml-Datei benötigt.

Beispiel für eine yaml-Datei, die einen nginx mit 2 Replikas erstellt:

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: nginx
spec:
  selector:
    matchLabels:
      app: nginx
  replicas: 2
  template:
    metadata:
      labels:
        app: nginx
    spec:
      containers:
      - name: nginx
        image: nginx
        ports:
        - containerPort: 80
---
apiVersion: v1
kind: Service
metadata:
  name: nginx
  labels:
    app: nginx
spec:
  ports:
  - name: http
    port: 80
    protocol: TCP
    targetPort: 80
  selector:
    app: nginx
  type: LoadBalancer
```

Aktivieren der yaml-Datei:

```powershell
kubectl apply -f .\aks_simple.yaml
```

Um anschließend die externe IP-Adresse des Services zu bekommen, kann der folgende Befehl ausgeführt werden (dauert aber ein paar Minuten, bis die IP provisioniert wurde):

```powershell
kubectl get services
```

Das K8s-Dashboard kann mit folgendem Befehl geöffnet werden:

```bash
az aks browse --resource-group TestRG --name TestAKS
```

#### create container images for solutions

Als nächstes wird ein Docker-Image erstellt :-) Der Code befindet sich in [https://github.com/stenet/az-203-prep/tree/master/custom-nginx](https://github.com/stenet/az-203-prep/tree/master/custom-nginx) inkl. dem nachfolgenden Dockerfile.

```dockerfile
FROM nginx

WORKDIR /usr/share/nginx/html
COPY ./www .

EXPOSE 80 443

CMD ["nginx", "-g", "daemon off;"]
```

Zusätzlich, wenn notwendig, kann mit dem Befehl "RUN" im Zuge des Buildvorganges andere Commands ausgeführt werden.

Mit der PowerShell in den Ordner wechseln, wo das Dockerfile ist und folgenden Befehl ausführen:

```powershell
docker build . -t custom-nginx:latest
docker run -p 8099:80 custom-nginx
```

Anschließend kann im Browser die URL localhost:8099 geöffnet werden.

#### publish an image to Azure Container Registry

Nachfolgend der Code, um das vorherige Beispiel in einer Azure Container Registry zu speichern.

Zuerst wird eine Registry erstellt:

```powershell
$acr = New-AzContainerRegistry `
  -ResourceGroupName TestRG `
  -Name TestACR20200129 `
  -Sku Standard
  -EnableAdminUser

$loginserver = $acr.LoginServer

az acr login --name TestACR20200129

docker tag custom-nginx $loginserver/custom-nginx:v1
docker push $loginserver/custom-nginx:v1
```

Tada, und schon ist das Image in unserem privaten Repository und kann verwendet werden ;-)

#### run containers by using Azure Container Instance or AKS

Wie ein Container in AKS gestartet werden kann, wurde bereits vorher beschrieben. Hier noch die Alternative mit Azure Container Instance. Hier sollte noch erwähnt werden, dass es nicht sinnvoll ist, einen Container so zu starten und durchgehend laufen zu lassen, da dies ziemlich ins Geld geht ;-)

```powershell
$cred = Get-AzContainerRegistryCredential `
  -ResourceGroupName TestRG `
  -Name TestACR20200129

$secpasswd = ConvertTo-SecureString $cred.Password `
  -AsPlainText `
  -Force

$cred2 = New-Object `
  -TypeName System.Management.Automation.PSCredential `
  -ArgumentList @($cred.Username, $secpasswd)

New-AzContainerGroup `
  -ResourceGroupName TestRG `
  -Name testaci20200129 `
  -Image $loginserver/custom-nginx:v1 `
  -IpAddressType Public `
  -OsType Linux `
  -Port @(80) `
  -RegistryCredential $cred2
```

## Develop Azure Platform as a Service compute solutions

### Create Azure App Service Web Apps

#### create an Azure App Service Web App

Bei der Verwendung von Visual Studio ist das ganze sehr einfach. Man erstellt eine neue ASP.NET Core Web Application und klickt sich durch. Beim Veröffentlichen der App nimmt einem der Assistent die ganze Arbeit ab.

Beispiel ist unter [https://github.com/stenet/az-203-prep/tree/master/vs/AzAppService](https://github.com/stenet/az-203-prep/tree/master/vs/AzAppService).

#### create an Azure App Service background task by using WebJobs

Im vorherigen Beispiel-Code steckt auch ein WebJobs-Projekt. Dieses ändert alle 5 Sekunden den Wert in einer Datei. Die Web App zeigt auf der Startseite diesen Wert an. Dies zeigt, dass sowohl die App als auch der WebJob in der gleichen Instanz laufen.

Kleiner Hinweis (der mich zwei Stunden gekostet hat): auch wenn sowohl die Web App als auch der WebJob in der gleichen Instanz laufen, teilen sie sich nicht das Temp-Verzeichnis. Dieses befindet sich zwar jeweils unter d:\local\temp, ist separat gemounted.

Es gibt zwei Arten von WebJobs-Types:

* Continuos - wird direkt gestartet, wenn der WebJob erstellt wird; läuft in allen Instanzen, in denen die Web App läuft (optional kann dies konfiguriert werden); unterstützt Remote Debugging
* Triggered - manueller Start oder zeitgesteuert; läuft auf einer einzigen Instanz; kein Remote Debugging

#### enable diagnostics logging

Bei Web Apps, die unter Windows laufen, können div. Loggings aktiviert werden. Hierfür im Portal die Web App öffnen und auf "App Service Logs" klicken. Dort können u.a. folgende Logs aktiviert werden:

* Application
* Web server
* Detailed error messages
* Failed request tracking

Je Typ werden die Daten in entsprechenden Orten abgelegt (Dateisystem, Blob, FTP).

Bei Linux kann nur "Application logging" aktiviert werden.

Weiters ist es möglich unter "Diagnostic settings" die Logs an Azure Monitor weiterzuleiten, welches das zentrale Modul für Alert und Metriken darstellt.

#### create an Azure Web App for containers

Docker Container können neben K8s und Azure Container Instance auch noch in App Services laufen.

Nachfolgend wird als erstes ein Service Plan erstellt und anschließend ein nginx Docker Container in diesem gestartet.

```powershell
az appservice plan create `
  -g TestRG `
  -l "West Europe" `
  -n plan20200129 `
  --sku S1 `
  --is-linux

az webapp create `
  -g TestRG `
  -p plan20200129 `
  -n nginx20200129 `
  -i nginx
```

Falls es eine eigene Domäne für die Web App gibt, dann kann diese noch mit folgendem Befehl hinzugefügt werden:

```powershell
az webapp config hostname add `
  -g TestRG `
  --webapp-name nginx20200129 `
  --hostname domain.com
```

Für ein Continuous Deployment von GitHub muss zusätzlich noch folgendes ausgeführt werden:

```powershell
$token = "GITHUB_ACCESS_TOKEN";

az webapp deployment source config `
  -g TestRG `
  --name nginx20200129 `
  --repo-url "https://github.com/myrepo" `
  --branch master `
  --git-token $token
```

Soll kein automatischer Sync zwischen GitHub und der Web App gemacht werden, kann zusätzlich das Flag "--manual-integration" angegeben werden. Um einen Sync auszulösen, wird der folgende Befehl verwendet:

```powershell
az webapp deployment source sync `
  -g TestRG `
  --name nginx20200129
```

Ein weiteres cooles Feature in Web Apps sind Slots. Eine Web App kann in mehreren Slots sein (z.B. ein Test-Slot und ein Produktiv-Slot). Um Slots zu erzeugen wird folgender Befehl ausgeführt:

```powershell
az webapp deployment slot create `
  -g TestRG `
  --name nginx20200129 `
  --slot slot20200129
```

Um TLS mutual authentication zu konfigurieren im Anschluss noch folgenden Befehl ausführen:

```powershell
az webapp update `
  -g TestRG `
  --name nginx20200129 `
  --set clientCertEnabled=true
```

Das Zertifikat wird im Request im X-ARR-ClientCert Header Base64 enkodiert übermittelt (https vorausgesetzt!) bzw. in ASP.NET in HttpRequest.ClientCertificate.

Das Beispiel habe ich mit der Azure CLI gemacht, da das PowerShell Cmdlet keine Möglichkeit hatte einen Linux Service Plan zu erstellen. 

#### monitor service health by using Azure Monitor

Azure Monitor ist das zentrale Monitoring Tool, das Telemetriedaten aus Azure-Ressourcen aber auch aus lokalen Prozessen sammelt und visualisiert.

![Überblick K8s](images/az_monitor_overview.jpg)

[https://azure.microsoft.com/de-de/services/monitor/](https://azure.microsoft.com/de-de/services/monitor/)

Zusätzlich werden Daten aggregiert. Mit Alerts können bei eintreten von bestimmten Bedingungen Personen z.B. per Email oder SMS informiert oder sonstige Aktionen (Azure Function, Azure LogicApp, ...) ausgelöst werden. 

### Create Azure App Service mobile apps

#### add push notifications for mobile apps

Nicht selbst ausprobiert, aber hier die Kurzfassung aus der Dokumentation:

In Azure einen "Notification Hub" erstellen. Sobald dieser erstellt ist, diesen öffnen und das Apple, Google oder weiß der Geier was Konto hinzufügen ;-)

Im Server-Code das NuGet Package "Microsoft.Azure.NotificationHub" hinzufügen, einen NotificationHubClient erstellen und dort die passende Send***-Methode aufrufen:

* SendAppleNativeNotificationAsync - Apple (iOS)
* SendFcmNativeNotificationAsync - Firebase (Android)
* SendWindowsNativeNotificationAsync - Windows

#### enable offline sync for mobile app

Offline data sync ist eine Client und Server SDK, welche im Client-Bereich für Android, Cordova, iOS, Xamarin und UWP zur Verfügung steht.

Damit wird es auf einfache Weise ermöglicht die App auch Offline bedienbar zu machen, in dem Änderungen lokal gespeichert und zu einem späteren Zeitpunkt synchronisiert werden.

Funktionsweise Offline Synchronisation:

* Push - Neuanlage, Änderungen und Löschvorgänge werden vom Client an den Server übertragen.
* Pull - pro Tabelle werden die Daten vom Server geholt.
* Implicit Pushes - falls für eine Tabelle ein Pull ausgeführt wird, für die noch Daten für einen Push vorhanden sind, dann wird zuerst der Push durchgeführt.
* Incremental Sync - beim Pull werden nicht alle Daten der Tabelle geholt, sondern nur die, die sich seit dem letzten Mal geändert haben. Dafür der Server ein updatedAt liefern
* Purging - damit werden die Daten einer Tabelle gelöscht. Falls zu diesem Zeitpunkt noch Daten zum Pushen vorhanden sind, wird eine Exception geworfen (außer es wird "force purge" angegeben).

#### implement a remote instrumentation strategy for mobile devices

### Create Azure Service API apps

#### create an Azure App Service API app

Eigentlich ist mir hier nicht ganz klar, was das sein soll. Ich verstehe darunter nichts anderes als eine Web API Anwendung, die z.B. mit ASP.NET Core erstellt und einem Service Plan veröffentlicht wird. Und da das bereits weiter oben beschrieben wurde, kommt hier nichts mehr ;-).

#### create documentation for the API by using open source and other tools

Hier geht es primär um den Einsatz von OpenAPI (früher Swagger). Dabei handelt es sich um eine Spezifikation zum Beschreiben von REST APIs, die sowohl von Menschen als auch Computern verstanden werden kann.

Um eine OpenAPI-Dokumentation für eine ASP.NET Anwendung erstellen zu können, kann z.B. mit NuGet das Swashbuckle-Packet installiert werden. Dieses liest die Kommentare einer Methode sowie summary/remark/return/param/response aus und erstellt dadurch die Beschreibung.

TODO Beschreibung Erstellen OpenAPI für Azure Functions

### Implement Azure functions

Mit Azure Functions können Funktionen veröffentlicht werden. Diese können z.B. über ein Assembly zur Verfügung gestellt oder aber z.B. direkt im Azure Portal erstellt werden. Aktiviert werden können sie z.B. durch Webhooks, Timers, Service Bus, Event Grid, ... Jede Function benötigt genau einen solchen Trigger.

Als Sprachen zur Erstellung von Functions steht .NET Core, Node.js, Python, Java und PowerShell Core zur Verfügung.

#### implement input and output bindings for a function

Bindings ermöglichen die einfache Verwendung von zusätzliche Ressourcen (z.B. Blog-Storage, Cosmos DB, ...). Dabei wird zwischen Input- und Output-Bindings unterschieden. Aufgrund des Namens dürfte die Bedeutung klar sein ;-).

Hier ein Beispiel für einen Trigger:

```csharp
[FunctionName("BlogTrigger")]        
public static void Run([BlobTrigger("xyz/{name}")] Stream blob, string name, ILogger log)
{
  log.LogInformation($"Es wurde eine neue Datei in den Container xyz mit dem Namen {name} und der Größe Size: {myBlob.Length} Bytes hinzugefügt.");
}
```
Dieser wird ausgeführt, wenn im definierten Storage-Account im Container "xyz" eine Datei hinzugefügt wird. "xyz/{name}" ist eine Binding Expression ([https://docs.microsoft.com/en-us/azure/azure-functions/functions-bindings-expressions-patterns](https://docs.microsoft.com/en-us/azure/azure-functions/functions-bindings-expressions-patterns)).

Mit dem Parameter "blob" bekommen wird den direkten Zugriff auf den Blob der erstellt wurde. 

Wird die Function im Azure Portal erstellt, dann fallen die Attribute wie "FunctionName", "BlobTrigger" weg, da diese bereits durch die Definition bekannt/definiert sind.

Kleine Zusatzbemerkung: Functions, die durch Blob storage, Queue Storage oder Service Bus aufgerufen werden und fehlschlagen, werden automatisch bis zu 5x erneut versucht. Falls es dann immer noch geklappt hat, landet die im Falle von Queue Storage und Service Bus die Messsage in der "poison queue".

Falls gleichzeitig mehreren Daten erstellt wurden, dann läuft die "Run"-Methode ggf. mehrfach parallel.

#### implement function triggers by using data operations, timers, and webhooks

TODO

#### implement Azure Durable Functions

Normale Azure Functions sind stateless. Dies bedeutet, dass diese ausgeführt werden und fertig. Mit Durable Functions verhält es sich leicht anders. Diese haben u.a. die Möglichkeit andere Azure Functions aufzurufen (inkl. Ergebnisabfrage) und auf Events zu erwarten. Dabei kann die Durable Function auch längere Zeit in einem Wartemodus verharren. 

```csharp
[FunctionName("Chaining")]
public static async Task<object> Run([OrchestrationTrigger] IDurableOrchestrationContext context)
{
  try
  {
    var x = await context.CallActivityAsync<object>("F1", null);
    var y = await context.CallActivityAsync<object>("F2", x);
    var z = await context.CallActivityAsync<object>("F3", y);
    return  await context.CallActivityAsync<object>("F4", z);
  }
  catch (Exception)
  {
    // Error handling or compensation goes here.
  }
}
```

Im Vergleich zu normalen Azure Functions ist der Rückgabewert bei Durable Functions ein Task. Weiters ist ein Parameter vom Type IDurableOrchestrationContext mit dem OrchestrationTriggerAttribute vorhanden. Im oberen Code werden die Funktionen F1 bis F4 hintereinander aufgerufen, wobei immer das Ergebnis der einen Funktion der nächsten Übergeben wird.

Ein anderes, sehr interessantes Beispiel gibt es unter [https://docs.microsoft.com/en-us/azure/azure-functions/durable/durable-functions-phone-verification](https://docs.microsoft.com/en-us/azure/azure-functions/durable/durable-functions-phone-verification). Hier geht es um den Versand einer Verifizierungs-SMS, die vom Benutzer bestätigt werden muss.

#### create Azure Function apps by using Visual Studio

Um Azure Function apps in Visual Studio erstellen zu können, muss bei der Installation von Visual Studio der "Azure Development" Workload installiert werden.

Bei der Erstellung des VS-Projektes das "Azure Functions"-Template auswählen.

Wenn die Function fertig programmiert ist, kann sie mittels "Publish" in Azure publiziert werden.

Ein Beispiel ist unter [https://github.com/stenet/az-203-prep/tree/master/vs/AzuFunctionApp](https://github.com/stenet/az-203-prep/tree/master/vs/AzuFunctionApp).

#### implement Python Azure functions

Diese werden am einfachsten mit Visual Studio Code erstellt. Folgende weiteren Dinge sollten installiert sein:

* Azure Functions Core Tools
* Azure Function extension in VSCode

Anschließend kann Function mit Hilfe von VSCode erstellt und veröffentlicht werden.

Ein Beispiel ist unter [https://github.com/stenet/az-203-prep/tree/master/vs/AzuFunctionAppPy](https://github.com/stenet/az-203-prep/tree/master/vs/AzuFunctionAppPy).

![Azure Function Pyhton](images/az_functions_py.png)

[https://docs.microsoft.com/en-us/azure/python/tutorial-vs-code-serverless-python-02](https://docs.microsoft.com/en-us/azure/python/tutorial-vs-code-serverless-python-02)

## Develop for Azure storage

### Develop solutions that use storage tables

#### design and implement policies for tables

Neben shared access signatures (SAS) gibt es serverseitig noch stored access policies. Damit können Startzeit, Endezeit und/oder Berechtigungen gesetzt werden. Folgende Storage-Ressourcen werden unterstützt:

* Blob containers
* File shares
* Queues
* Tables

Bei der Erstellung einer SAS kann entweder die Berechtigung direkt angegeben werden, oder auf eine Policy verwiesen werden. Der Vorteil beim Verweis auf die Policy ist, dass diese im Nachhinein geändert oder gelöscht werden kann, wodurch die Rechte entzogen werden. Da eine SAS nur eine Signatur ist, die serverseitig nicht gespeichert ist, ist diese so lange gültig, wie beim Erstellen definiert wurde.

Wichtig: das Erstellen einer Policy kann bis zu 30 Sekunden dauern!

Nachfolgend der Code für das Erstellen eines Storage-Accounts, eines Containers und für das Hochladen einer Datei.

```powershell
$storage = New-AzStorageAccount `
  -ResourceGroupName TestRG `
  -Name storage20200201 `
  -Location "West Europe" `
  -SkuName Standard_LRS

New-AzStorageContainer `
  -Context $storage.Context `
  -Name container01 `
  -Permission Off

$file = Set-AzStorageBlobContent `
  -Context $storage.Context `
  -Container container01 `
  -File c:\temp\bild.jpg

$file.ICloudBlob.Uri.AbsoluteUri
```

Die Bild.jpg Datei ist jetzt hochgeladen. Allerdings hat niemand Rechte die Datei zu laden.

Bei den Berechtigungen stehen folgende Werte zur Auswahl:

* off - privat, keine Berechtigung
* blog - öffentliche Leseberechtigung für Blobs
* container - öffentliche Leseberechtigung für Container inkl. ermitteln aller Blogs im Container

Nachfolgend der Code zum Erstellen einer stored access policy:

```powershell
New-AzStorageContainerStoredAccessPolicy `
  -Context $storage.Context `
  -Container container01 `
  -Permission r `
  -ExpiryTime 2020-02-02 `
  -Policy perm01

$sas = New-AzStorageContainerSASToken `
  -Context $storage.Context `
  -Container container01 `
  -Policy perm01

$file.ICloudBlob.Uri.AbsoluteUri + $sas
```

Alternativ kann ein SAS Token ohne Referenz auf eine Policy erstellt werden:

```powershell
$sas2 = New-AzStorageContainerSASToken `
  -Context $storage.Context `
  -Container container01 `
  -Permission r `
  -ExpiryTime 2020-02-02

$file.ICloudBlob.Uri.AbsoluteUri + $sas2
```

Abschließend sei erwähnt, dass max. 5 stored access policies pro Element definiert werden können!

#### query table storage by using code

Nachfolgend wird ein neuer Table-Storage inkl. einem SAS-Token mit vollen Rechten erstellt:

```powershell
$storage = New-AzStorageAccount `
  -ResourceGroupName TestRG `
  -Name storage202002012 `
  -Location "West Europe" `
  -SkuName Standard_LRS

$table = New-AzStorageTable `
  -Context $storage.Context `
  -Name table01

$sas = New-AzStorageTableSASToken `
  -Context $storage.Context `
  -Table table01 `
  -Permission radu

$table.Uri.AbsoluteUri + $sas
```

Mit einem GET-Request auf die URL, die das obige Skript erstellt, kann der Table Storage abgefragt werden. Standardmäßig kommen die Ergebnisse in XML zurück. Um JSON zu erhalten, muss der Accept-Header mit "application/json" hinzugefügt werden. Aus OData werden die folgenden Abfrageoptionen unterstützt:

* $filter
* $top
* $select

Mit einem POST-Request können Daten hinzugefügt werden. Dabei ist zu beachten, dass PartionKey und RowKey immer angegeben werden müssen!

Alternativ hier der Code in C#:

```powershell
var storageAccount = CloudStorageAccount.Parse(CONNECTION_STRING);
var tableClient = storageAccount.CreateCloudTableClient();

var table = tableClient.GetTableReference("test");
table.CreateIfNotExistsAsync();

var person = new PersonEntity()
{
  PartitionKey = "AT",
  RowKey = "1",
  FirstName = "A",
  LastName = "B"
};

var insertOperation = TableOperation.Insert(person);
table.ExecuteAsync(insertOperation).Wait();

person.LastName = "X";
var replaceOperation = TableOperation.InsertOrReplace(person);
table.ExecuteAsync(replaceOperation).Wait();

var condition = TableQuery.CombineFilters(
  TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "AT"),
  TableOperators.And,
  TableQuery.GenerateFilterCondition("LastName", QueryComparisons.Equal, "X"));

var query = new TableQuery<PersonEntity>().Where(condition);
var result = table.ExecuteQuerySegmentedAsync(query, null).Result;

var personResult = result.Results[0];
```

Wenn mehrere Änderungen innerhalb einer Tabelle gemacht werden müssen, dann kann auch eine Instanz von TableBatch erstellt werden, dort alle Änderungen eingefügt, und diese dann mit ExecuteBatchAsync ausgeführt werden.

Bei einem Query werden max. 1000 Datensätze zurückgegeben. Alternativ kann mit query.Take(20) angegeben werden, dass weniger zurückgegeben werden sollen.

PersonEntity ist eine Klasse, die von TableEntity erbt. Das ganze Beispiel ist unter [https://github.com/stenet/az-203-prep/tree/master/vs/AzStorageTable](https://github.com/stenet/az-203-prep/tree/master/vs/AzStorageTable).

#### implement partitioning schemes

Beim vorherigen Punkt wurde schon einmal der PartitionKey erwähnt. Dieser spielt beim Table Storage und auch Cosmos DB eine ganze entscheidende Rolle.

Partitionen sind wichtig für die Skalierbarkeit und Performance. Unterschiedliche Partitionen können auf unterschiedliche Host-Systeme verteilt werden.

Es gibt drei Standard-Strategien für das partitionieren von Daten:

* horizontal (sharding) - jede Partition ist eine separate "Datenbank", aber alle Partitionen haben das gleiche Schema. Die Daten werden somit in Gruppen unterteilt (z.B. Land oder Benutzer).
* vertikal - im Vergleich zu horizontal werden hierbei keine Gruppen gebildet, sondern die Datensätze werden auseinander genommen. Oft benötigte Felder sind in einer Partition, nicht oft verwendete in einer anderen.
* functional - hierbei werden Daten aggregiert und nach benötigten Kontexten aufgebaut.

Ein paar Punkte als Entscheidungshilfe:

* Cross-Partitionszugriffe sollten vermieden werden (ev. im Notfall Daten redundant speichern)
* es sollte nicht so sein, dass eine Partition 95 % der Daten enthält und der Rest in anderen Partionen liegt

Der Table Storage unterstützt nur den Index PartitionKey + RowKey. Cosmos DB mit der Table Storage API erstellt Indexe automatisch bei Bedarf.

### Develop solutions that use Cosmos DB storage

Cosmos DB ist eine global verteilte, Multi-Model, No-SQL Datenbank. Es werden folgende APIs unterstützt:

* SQL
* MongoDB
* Casandara
* Table Storage
* Gremlin

#### create, read, update, and delete data by using appropriate APIs

Zuerst einen Cosmos DB Account erstellen:

```powershell
az cosmosdb create `
  -g TestRG `
  -n cosmos20200201
```

Im Visual Studio-Projekt das NuGet-Paket Microsoft.Azure.DocumentDB.Core hinzufügen. Der untere Code befindet sich in [https://github.com/stenet/az-203-prep/tree/master/vs/AzuCosmosDB](https://github.com/stenet/az-203-prep/tree/master/vs/AzuCosmosDB).

```csharp
var client = new DocumentClient(new Uri(endpoint), key);

var databaseLink = UriFactory.CreateDatabaseUri(DATABASE_ID);
var collectionLink = UriFactory.CreateDocumentCollectionUri(DATABASE_ID, COLLECTION_ID);

var database = await client.CreateDatabaseIfNotExistsAsync(new Database()
{
  Id = DATABASE_ID
});

var collection = new DocumentCollection()
{
  Id = COLLECTION_ID
};

collection.PartitionKey.Paths.Add("/Country");
collection = await client.CreateDocumentCollectionIfNotExistsAsync(databaseLink, collection);

var person1 = new Person("A", "A", "AT");
await client.UpsertDocumentAsync(collectionLink, person1);

var person2 = new Person("B", "B", "AT");
await client.UpsertDocumentAsync(collectionLink, person2);

var person3 = new Person("C", "C", "AT");
await client.UpsertDocumentAsync(collectionLink, person3);

var person4 = new Person("D", "D", "DE");
await client.UpsertDocumentAsync(collectionLink, person4);

var allPersonList = client
  .CreateDocumentQuery<Person>(collectionLink)
  .ToList();

var personWithFirstNameDList = client
  .CreateDocumentQuery<Person>(collectionLink, new FeedOptions()
  {
    EnableCrossPartitionQuery = true
  })
  .Where(c => c.FirstName == "D")
  .ToList();

foreach (var item in personWithFirstNameDList)
{
  item.FirstName = "DD";

  await client.ReplaceDocumentAsync(
    UriFactory.CreateDocumentUri(DATABASE_ID, COLLECTION_ID, item.Id),
    item,
    new RequestOptions() { PartitionKey = new PartitionKey(item.Country) });
}

var personWithPartitionKeyATList = client
  .CreateDocumentQuery<Person>(collectionLink, new FeedOptions()
  {
    PartitionKey = new PartitionKey("AT")
  })
  .ToList();

foreach (var item in personWithPartitionKeyATList)
{
  await client.DeleteDocumentAsync(
    UriFactory.CreateDocumentUri(DATABASE_ID, COLLECTION_ID, item.Id),
    new RequestOptions() { PartitionKey = new PartitionKey(item.Country) });
}
```

#### implement partitioning schemes

siehe Table Storage ...

#### set the appropriate consistency level for operations

Cosmos DB unterscheidet 5 Konsistenzebenen (in absteigender Reihenfolge):

* Strong - es ist sichergestellt, dass die Daten in allen Read-Regionen den gleichen Status haben. 
* Bounded-Staleness - spätestens nach in den Einstellung hinterlegten Zeitspanne bzw. Operationen sind die Daten in allen Read-Regionen aktiv. Die Reihenfolge der Schreibvorgänge bleibt erhalten.
* Session (Default) - Daten die innerhalb eines Session geschrieben werden, sind in derselben Session in anderen Regionen in der identischen Reihenfolge lesbar (= Strong). Bei anderen Session ist zwar die Reihenfolge ident, aber ggf. zeitlich versetzt.
* Consistent Prefix - die Reihenfolge der Schreibvorgänge bleibt erhalten. Allerdings können die Ergebnisse in unterschiedlichen Regionen zum gleichen Zeitpunkt unteschiedlich sein.
* Eventual - auf gut Deutsch: es kommt was grad da ist ;-)

Diese spielen primär dann eine Rolle, wenn Daten georepliziert (gibt es das Wort?) wurden.

Der Konsistenzlevel kann je Client oder Request geändert werden, allerdings nur zu einem Schwächeren als den, der als Standard definiert ist.

### Develop solutions that use relational database

In Azure gibt es mehrere Möglichkeiten eine SQL-Datenbank zu erstellen:

* klassisch einen virtuellen Server mit einer SQL-Server-Instanz
* SQL Database Managed Instance
* SQL Database

Einer der Vorteile bei den zwei zuletzt genannten ist, dass sich Microsoft um die Patches und neuen Versionen kümmert.

#### provision and configure relational databases

```powershell
New-AzSqlServer `
  -ResourceGroupName TestRG `
  -Location "West Europe" `
  -ServerName sql20200203

New-AzSqlServerFirewallRule `
  -ResourceGroupName TestRG `
  -ServerName sql20200203 `
  -FirewallRuleName fw01 `
  -StartIpAddress 0.0.0.0 `
  -EndIpAddress 0.0.0.0

New-AzSqlDatabase `
  -ResourceGroupName TestRG `
  -ServerName sql20200203 `
  -DatabaseName sql `
  -RequestedServiceObjectiveName S2 
```

#### configure elastic pools for Azure SQL Database

Ein elastischer Pool ermöglicht es z.B. zusätzliche DTUs zu reservieren und diese mehreren SQL-Datenbanken zuzuweisen. Damit können ev. Spitzen in einzelnen Datenbanken durch den Pool ausgeglichen werden.

Nachfolgend wird ein Pool erstellt und der zuvor erstellten Datenbank zugewiesen.

```powershell
New-AzSqlElasticPool `
  -ResourceGroupName TestRG `
  -ElasticPoolName pool01 `
  -ServerName sql20200203 `
  -Dtu 50

Set-AzSqlDatabase `
  -ResourceGroupName TestRG `
  -ServerName sql20200203 `
  -DatabaseName sql `
  -ElasticPoolName pool01  
```

#### create, read, update, and delete tables by using code

zu Basic ;-)

Allerdings gibt es doch ein paar Punkte, die generell interessant sind, die nachfolgend behandelt werden.

Data Masking ermöglicht es einzelne Spalten für unberechtigte Benutzer zu verschleiern. Dafür gibt es unterschiedliche Masken:

* Default - Text ergibt XX oder XXXX, Nummer ergibt 0, Datum ergibt 01.01.1900
* Email - ergibt den ersten echten Buchstaben + .com am Ende. Beispiel: sXXXX@XXXX.com
* Random - bei Nummern irgendeine Zufallszahl
* Custom string - Präfix + Padding + Suffix. Beispiel: sXXXXXXXXXm

Die Definition kann bei der Anlage der Spalte oder später mit einem ALTER COLUMN-Befehl gemacht werden:

```sql
alter table [Person] alter column [Test] masked with (function = 'default()')
alter table [Person] alter column [Email] masked with (function = 'email()')
alter table [Person] alter column [Alter] masked with (function = 'random(1, 99)')
alter table [Person] alter column [Alter] masked with (function = 'partial(0, "XXX-XX-X", 4)')
```

Alternativ kann in Azure auch ein PowerShell-Befehl abgesetzt werden:

```powershell
New-AzSqlDatabaseDataMaskingRule `
  -ResourceGroupName TestRG `
  -ServerName server20200205 `
  -DatabaseName database01 `
  -SchemaName dbo `
  -TableName Person `
  -ColumnName Test `
  -MaskingFunction Default
```

Damit Benutzer die Daten ohne Maske sehen, benötigen sie die "UNMASK"-Berechtigung.
Übrigens, obwohl ein Benutzer den echten Inhalt nicht sieht, kann er diesen (vorausgesetzt er hat die Rechte zum Schreiben) ändern!

Ein weitere nettes Feature ist "always encrypted". Ziel ist es sensible Daten wie z.B. Kreditkartennummern, die in der Datenbank gespeichert sind, zu verschlüsseln. Dabei bekommt die Datenbank den unverschlüsselten Wert selbst nie zu sehen. Für das verschlüsseln und entschlüsseln ist der Client-Treiber zuständig.

Es gibt zwei Typen von Verschlüsselungen:

* deterministic - hierbei ist ein entschlüsselter Wert immer der gleiche verschlüsselte Wert. Dadurch kann, wenn man bei einem Datensatz die Bedeutung dieses Wertes kennt, dies auf die anderen Datensätze mit gleichem Wert abgeleitet werden (z.B. sehr problematisch bei bool Spalten). Damit sind aber Lookups, Joins und Group by weiterhin möglich.
* randomized - hier nicht :-)

Es gibt zwei Schlüssel, die hierfür notwendig sind:
* Master Key (CM) - dieser wird extern (z.B. KeyVault) gespeichert.
* Encryption Key - mit diesem werden die Daten einer Spalte verschlüsselt. Der Encryption Key ist verschlüsselt (mit dem Master Key) im SQL-Server gespeichert. Damit ist auch klar, dass der SQL-Server selbst die Daten nicht entschlüsseln kann. 

#### provision and configure Azure SQL Database serverless instances

wurde weiter oben schon behandelt ...

#### provision and configure Azure SQL and Azure PostgreSQL Hyperscale instances

Die Hyperscale Editionen sind die Luxuseditionen für gehobene Ansprüche ;-)

* bis 100TB Datenbankgröße
* nahezu Echtzeit Backups
* schnelles Wiederherstellen von Datenbanken (Minuten statt Stunden)
* Mega Performance
* schnelles Scale out + up
* ziemlich teuer (startet bei € 440 für die kleinste Edition und geht bis knapp € 18.000 pro Monat)

Das erstellen ist aber soweit ich sehe nicht anders als bei einer anderen Datenbank.

### Develop solutions that use blob storage

#### move items in Blob storage between storage accounts or container

```powershell
$storage = New-AzStorageAccount `
  -ResourceGroupName TestRG `
  -Name storage20200201 `
  -Location "West Europe" `
  -SkuName Standard_LRS

New-AzStorageContainer `
  -Context $storage.Context `
  -Name container01 `
  -Permission Off

New-AzStorageContainer `
  -Context $storage.Context `
  -Name container02 `
  -Permission Off

$file = Set-AzStorageBlobContent `
  -Context $storage.Context `
  -Container container01 `
  -File c:\temp\bild.jpg

Start-AzStorageBlobCopy `
  -Context $storage.Context `
  -SrcContainer container01 `
  -SrcBlob bild.jpg `
  -DestContext $storage.Context `
  -DestContainer container02

Remove-AzStorageBlob `
  -Context $storage.Context `
  -Container container01 `
  -Blob bild.jpg
```

#### set and retrieve properties and metadata

```powershell
$storage = New-AzStorageAccount `
  -ResourceGroupName TestRG `
  -Name storage20200201 `
  -Location "West Europe" `
  -SkuName Standard_LRS

New-AzStorageContainer `
  -Context $storage.Context `
  -Name container01 `
  -Permission Off

$file = Set-AzStorageBlobContent `
  -Context $storage.Context `
  -Container container01 `
  -File c:\temp\bild.jpg

$file.ICloudBlob.Metadata.Add("Name", "Müller")
$file.ICloudBlob.SetMetadata()
```

#### implement blob leasing

Mit Leasing können Blob für Schreib- und Löschvorgänge für Benutzer, die keinen Lease besitzen, gesperrt werden.

Es gibt 5 Lease-Operationen:

* Aquire - einen neuen Lease anfragen
* Renew - bestehenden Lease erneuern
* Change - ID des bestehenden Lease ändern
* Release - Lease freigeben
* Break - Lease beenden, aber solange gesperrt lassen, bis der Lease abgelaufen wäre

```powershell
$storage = New-AzStorageAccount `
  -ResourceGroupName TestRG `
  -Name storage20200201 `
  -Location "West Europe" `
  -SkuName Standard_LRS

New-AzStorageContainer `
  -Context $storage.Context `
  -Name container01 `
  -Permission Off

$file = Set-AzStorageBlobContent `
  -Context $storage.Context `
  -Container container01 `
  -File c:\temp\bild.jpg

$file.ICloudBlob.Properties.LeaseStatus  

$lease = $file.ICloudBlob.AcquireLease($null, $null, $null, $null, $null)
$file.ICloudBlob.ReleaseLease(@{LeaseId = $lease}, $null, $null)

$file.ICloudBlob.BreakLease()
```

#### implement data archiving and retention

Innerhalb eines Storage Account können im Bereich "Lifecycle Management" Regeln definiert werden, dass Blob nach einem bestimmten Zeitraum verschoben oder gelöscht werden sollen.

#### implement Geo Zone Redundant storage

Bei der Erstellung des Storage Accounts bei "Replication" GRS oder RA-GRS auswählen bzw. zu einem späteren Zeitpunkt ändern ;-) 

## Implement Azure Security

### Implement authentication

#### implement authentication by using certificates, forms-based authentication, or tokens

Benutzer und Passwort sind ja ziemlich out. Daher kommt jetzt ein Beispiel, wie sich eine Anwendung mittels eines lokal installierten Zertifikates anmelden kann. Das folgende Skript muss in der "normalen" PowerShell (also nicht Core) ausgeführt werden. Es erstellt ein Zertifikat und eine "test.cer"-Datei.

```powershell
$cert = New-SelfSignedCertificate -CertStoreLocation cert:\CurrentUser\My -Subject "CN=test" -KeySpec KeyExchange

$out = New-Object String[] -ArgumentList 3
$OutputFile = ".\test.CER"
 
$out[0] = "-----BEGIN CERTIFICATE-----"
$out[1] = [System.Convert]::ToBase64String($cert.GetRawCertData(), "InsertLineBreaks")  
$out[2] = "-----END CERTIFICATE-----"
 
$out > test.cer
```

Im Azure Portal unter "Azure Active Directory" => "App registrations" eine neue Registrierung machen und unter "Certificates & secrets" die zuvor erstellte "test.cer"-Datei hochladen. Als nächstes im SQL-Server die registrierte App als Benutzer hinzufügen (Access control (IAM)). Danach im Portal in die Datenbank wechseln, auf die die Rechte gewährt werden sollen und Query Editor öffnen. Hier muss die AD-Authentifizierung verwendet werden! Dort dann den folgenden Befehl eingeben:

```
CREATE USER [WIE_AUCH_IMMER_ICH_DIE_APP_BENANNT_HABE] FROM EXTERNAL PROVIDER
```

Das war es auf der Azure-Seite. Jetzt zum Client Code. Der Connection-String wird ohne UserId und Passwort erstellt. Stattdessen wird ein AccessToken erstellt und in der SqlConnection zugewiesen. Der Code zum Erstellen des AccessTokens liegt unter [https://github.com/stenet/az-203-prep/tree/master/vs/AzSqlAccessToken](https://github.com/stenet/az-203-prep/tree/master/vs/AzSqlAccessToken).

#### implement multi-factor or Windows authentication by using Azure AD

Für MFA wird Azure Active Directory Premium benötigt. 

Im Kontext von MFA kommt oft der Begriff "Conditional Access".

![Conditional Access](images/az_conditional_access.png)

[https://docs.microsoft.com/en-us/azure/active-directory/conditional-access/overview](https://docs.microsoft.com/en-us/azure/active-directory/conditional-access/overview)


Hierbei geht es darum, dass Anwendungen und primär Daten so gut wie möglich geschützt sind, ohne den Benutzer unnötig mit Sicherheitsabfragen zu belästigen. 

Nachfolgend ein sehr vereinfachtes Beispiel für eine Conditional Access Policy:

* Ein Benutzer meldet sich mit Benutzer + Passwort in seinem Email-Account an. 
* Macht er dies von dem Ort, von dem er es die letzten Male schon gemacht hat, dann wird er direkt angemeldet.
* Wenn nicht, dann muss er zusätzlich einen PIN eingeben, der im per SMS auf sein Telefon gesendet wurde.

Dabei können eine Vielzahl von Faktoren eine Rolle spielen:

* ist der Benutzer ein Administrator
* was für ein Gerät verwendet er
* ....

Dies kann auch soweit gehen, dass er zwar Zugriff bekommt, aber mitunter nur eingeschränkten.

#### implement OAuth2 authentication

Die nachfolgende Grafik veranschaulicht den Ablauf bei OAuth2.

![OAuth 2](images/az_security_oauth2.png)

[https://docs.microsoft.com/en-us/azure/active-directory/develop/v1-protocols-oauth-code](https://docs.microsoft.com/en-us/azure/active-directory/develop/v1-protocols-oauth-code)

Grundlage für den weiteren Ablauf ist die App Registrierung vom vorherigen Beispiel.

Als erstes muss, wie in der Grafik ersichtlich, eine Authorization-Code ermittelt werden. Hierfür den Benutzer auf folgende Seite weiterleiten:

https://login.microsoftonline.com/{ID_TENANT}/oauth2/authorize?client_id={ID_APPLICATION}ad&response_type=code&state={IRGENDWAS_WENN_MAN_WILL}

Der Benutzer gibt dort seine Credentials ein und gewährt den Zugriff auf die benötigten Daten. Anschließend wird er auf die Redirect URI in der App Registrierung weitergeleitet. In der Weiterleitung sind die Query Parameter "code" und "session_state".

Nun muss ein Post-Request an folgende Adresse gemacht werden:

https://login.microsoftonline.com/{ID_TENANT}/oauth2/token

Zusätzlich müssen die folgende Parameter als x-www-form-urlencoded übermittelt werden (JSON geht zumindest bei mir nicht ...):

* grant_type - "authorization_code"
* client_id - ID_APPLICATION
* client_secret - Secret in der App Registrierung unter "Certificates & secrets" bei "Client secrets"
* code - den Code, wenn wir vorher erhalten haben

Das Ergebnis ist ein JSON mit u.a. folgenden Werten:

* access_token
* refresh_token
* id_token
* expires_on

Bei zukünftigen Abfragen wird der Access-Token im Authorization-Header mit dem Wewrt "Bearer {ACCESS_TOKEN}".

Um einen neuen Access-Token mit Hilfe des Refresh-Tokens zu erstellen, muss der gleiche Request wie zuvor abgesendet werden, aber mit folgenden Parametern:

* grant_type - "refresh_token"
* client_id - ID_APPLICATION
* client_secret - Secret in der App Registrierung unter "Certificates & secrets" bei "Client secrets"
* refresh_token - Refresh Token von vorher

#### implement Managed identities/Service Principal authentication

Viele Ressourcen können selbst eine Identity sein. 

Am Beispiel App Services kann in den Einstellungen unter "Identity" der Ressource eine "System assigned" Identity zugewiesen werden (sprich genau für diese Ressource gibt es eine eigene Identity). Alternativ kann unter "User Assigned" eine zuvor erstellte "User Assigned Managed Identity" zugeteilt werden.

Damit entfällt, entsprechende Rechte auf die hinterlegte Identity vorausgesetzt, dass sich einzelne Ressourcen untereinander z.B. durch Passwörter authentifizieren müssen.

#### implement Microsoft identity platform

Laut [https://docs.microsoft.com/en-us/azure/active-directory/develop/v2-overview](https://docs.microsoft.com/en-us/azure/active-directory/develop/v2-overview) handelt es sich dabei um eine Evolution von Azure Active Directory.

Das hier geforderte ist (meiner Meinung nach) bereits im oberen Beispiel Zertifikat + SQL Server erstellt worden.

### Implement access control

Unterschied zwischen Role und Claim:

* Benutzer hat ein oder mehrere Rollen (z.B. Administrator, HR, Buchhaltung, ...)
* Claims sind Eigenschaften von Benutzern, wie z.B. Geburtsdatum, Name, Adresse, ... also ein Key/Value-Paar

Unter [https://docs.microsoft.com/en-us/aspnet/core/security/authorization/introduction?view=aspnetcore-3.1](https://docs.microsoft.com/en-us/aspnet/core/security/authorization/introduction?view=aspnetcore-3.1) ist das Thema sehr gut zusammengefasst.

#### implement CBAC (Claims-Based Access Control) authorization

Wie zuvor erwähnt geht es hier um Berechtigungen aufgrund von bestimmten Eigenschaften eines Benutzers. Ein Beispiel ist, dass ein bestimmtes Formular oder eine Funktion nur aufgerufen werden darf, wenn der Benutzer mindestens 18 Jahre alt ist.

```csharp
[Authorize(Policy = "AtLeast21")]
public class AlcoholPurchaseModel : PageModel
{
}
```

#### implement RBAC (Role-Based Access Control) authorization

Im Vergleich dazu kann mit RBAC definiert werden, dass ein bestimmtes Formular oder eine Funktion nur aufgerufen werden darf, wenn der Benutzer eine bestimmte Rolle besitzt.

```csharp
[Authorize(Roles = "Administrator")]
public class AdministrationController : Controller
{
}
```

#### create shared access signatures

Dieses Thema wurde bereits beim Thema stored access policies behandelt.

### Implement secure data solutions

#### encrypt and decrypt data at rest and in transit

Grundsätzlich sei erwähnt, dass alle Daten z.B. in einem Storage Account verschlüsselt sind. Dafür wird ein Microsoft interner Schlüssel verwendet. Es ist allerdings auch möglich einen eigenen zu verwenden (Storage Account => Encryption).

Bei Encryption in transit sind verschlüsselte Verbindungen (= TLS) entscheidend.

#### create, read, update, and delete keys, secrets, and certificates by using the KeyVault API

KeyVault ist das zentrale Azure Produkt, in dem alle Kennwörter, Zertifikate, ... gespeichert werden. 

```powershell
New-AzKeyVault `
  -ResourceGroupName TestRG `
  -Name kv20200203 `
  -Location "West Europe"

$secretVal = ConvertTo-SecureString `
  -String "asdfobjDFl4rn" `
  -AsPlainText `
  -Force 

$secret = Set-AzKeyVaultSecret `
  -VaultName kv20200203 `
  -Name MasterPassword `
  -SecretValue $secretVal
```

Hier der Code zum erstellen eines Keys:

```powershell
Add-AzKeyVaultKey `
  -VaultName kv20200203 `
  -Name MasterKey `
  -Destination Software
```

## Monitor, troubleshoot, and optimize Azure Solutions

### Develop code to support scalability of apps and services

#### implement autoscaling rules and patterns (schedule, operational/system metrics, singleton applications)

Im Azure Monitor im Menüpunkt "Autoscale" werden die Ressourcen aufgelistet, für die Autoscaling verfügbar bzw. aktiviert ist. Durch den Klick auf die Ressource (z.B. eine Service App) werden die Autoscale Eigenschaften angezeigt. Standardmäßig befinden sich diese auf "manual". Damit kann der Benutzer manuell z.B. die Azahl der Instanzen festlegen.

Um diese zu automatisieren muss auf "Custom autoscale" gewechselt werden. Hier können nun Regeln definiert werden, wann ein "scale out" und wann ein "scale in" durchgeführt werden soll. Dies kann abhängig von Zeiten oder Metriken sein.

Wichtig: immer auch eine "scale in" Regel definieren, damit, wenn Ressourcen nicht benötigt werden, diese nicht beansprucht und bezahlt werden müssen.

Unterschied zwischen horizontalem und vertikalem Scaling:

* Horizontal - neue Instanzen
* Vertikal - Instanz bekommt mehr Ressourcen

#### implement code that handles transient faults

In diesem Punkt geht es darum, wie im Falle von nicht verfügbaren Ressourcen (kurzfristig, langfristig) umgegangen werden soll. Ein paar Guidelines aus der Dokumentation [https://docs.microsoft.com/en-us/azure/architecture/best-practices/transient-faults](https://docs.microsoft.com/en-us/azure/architecture/best-practices/transient-faults):

* Prüfen, ob die Funktion im SDK selbst versucht, den Vorgang zu wiederholen.
* Prüfen, ob es überhaupt Sinn macht, den Vorgang zu wiederholen.
* Anzahl der erneuten Versuche und zeitraum dazwischen sinnvoll abwägen
* Protokollierung
* Szenarien testen!

#### implement AKS scaling strategies

Es gibt folgende Möglichkeiten:

* manuelle Skalierung von Pods und Nodes
* Horizontal Pod Autoscaler
* Cluster Autoscaler
* Einbindung Azure Container Instances

### Integrate caching and content delivery within solutions

#### store and retrieve data in Azure Redis cache

NuGet-Paket "StackExchange.Redis" einbinden. 

```csharp
var redis = ConnectionMultiplexer.Connect(CONNECTION_STRING);
var cache = redis.GetDatabase();

cache.StringSet("mykey", "WERT");
var value = cache.StringGet("mykey");
```

Gesamter Code (wobei es nicht viel mehr ist ;-)) ist unter [https://github.com/stenet/az-203-prep/tree/master/vs/AzRedisCache](https://github.com/stenet/az-203-prep/tree/master/vs/AzRedisCache).

#### develop code to implement CDN's in solutions

Anfragen gehen nicht direkt an primären Server, sondern an einen Edge Server (durch DNS an einen für den Anforderer gut gelegene POP (Point of Presence)). Dieser prüft, ob er die gewünschte Datei hat. Wenn ja und TTL noch nicht abgelaufen ist, wird diese retourniert. Wenn nichts, dann wird diese vom primären Server geholt, zwischengespeichert und dann retourniert.

#### invalidate cache content (CDN or Redis)

Bei Redis kann während des Erstellens eines Keys ein Zeitraum angegeben werden, nachdem der Key gelöscht wird.

Bei CDN kann dies über "Caching rules" gesteuert werden. Dabei wird zwischen zwei Typen unterschieden:

* Global caching rules - eine globale Rule pro Endpunkt; dieser überschreibt ggf. die Cache Headers
* Custom caching rules - eine oder mehrere Rules pro Endpunkt; beziehen sich auf Pfade oder Dateiendungen, werde der Reihe nach geprüft und überschreiben die globale Rule.

Dann gibt es pro Cache Rule noch Caching behaviors:

* Bypass cache - Keine Cache
* Override - Überschreibt die Cache Duration
* Set if missing - Behält die ursprünglichen Cache Headers; wenn fehlen wird Cache Duration ergänzt

Bzgl. Query Strings gibt es ebenfalls drei Varianten:

* Ignore query strings - falls die Datei noch nicht im Cache ist, wird sie inkl. dem Query-String vom primären Server geholt. Bei allen weiteren Requests wird der Query String einfach ignoriert
* Bypass caching for query string - wenn eine Query String enthalten ist, wird nicht gecacht
* Cache very unique URL - jede URL bekommt einen separaten Cache-Eintrag

### Instrument solutions to support monitoring and logging

#### configure instrumentation in an app or service by using Applications Insights

Wenn der "Azure Development" Workload installiert ist, dann ist die Einbindung von Application Insights in eine ASP.NET Anwendung sehr einfach. Einfach mit rechter Maustaste auf das Projekt klicken => Add => "Application Insights Telemetry...". Dann werden die notwendigen NuGet-Pakete installiert und alles konfiguriert. Im Root des Projektes befindet sich dann eine "ApplicationInsights.config"-Datei, in der u.a. auch der InstrumentationKey angegeben ist.

Um Application Insights in einer JavaScript-Anwendung zu integrieren, muss als erstes das Paket "@microsoft/applicationinsights-web" per npm installiert werden. Anschließend im Start den Code (nur als Beispiel) platzieren:

```javascript
const appInsights = new ApplicationInsights({ 
  config: {
    instrumentationKey: "MEIN_INSTRUMENTATION_KEY",
    enableCorsCorrelation: true,
    autoTrackPageVisitTime: true,
    enableAutoRouteTracking: true
  }
});

appInsights.loadAppInsights();
```

Nach dem Login folgenden Code ausführen:

```javascript
appInsights.setAuthenticatedUserContext(ID_BENUTZER);
```

Nach einem Logout folgenden Code ausführen:

```javascript
appInsights.clearAuthenticatedUserContext();
```

Es gibt unterschiedliche Track-Methoden:
* TrackPageView
* TrackEvent
* TrackMetric
* TrackException
* TrackRequest
* TrackTrace
* TrackDependency

Im Bereich "Usage" im Azure Portal gibt es u.a. folgende Inhalte:

* Users - wie viele Benutzer werden die App?
* Sessions - wie viele Sitzungen? Sitzungen werden nach 30 Minuten Inaktivität oder 24 Stunden beendet
* Events - wie oft wurden bestimmte Seite oder Features verwendet?
* Funnels - wie verwenden Benutzer die Anwendung (Ablauf, Prozess, Workflow)?
* User Flows - wie navigieren Benutzer zwischen Seiten und Features?
* Retention - wie viele Benutzer kommen zurück?
* Impact - wie bestimmten Ladezeit und andere Eigenschaften das Verhalten der Benutzer?

#### analyze and troubleshoot solutions by using Azure Monitor

Azure Monitor muss man sich selbst anschauen. Ist ziemlich selbsterklärend und hat jede Menge Features ...

#### implement Applications Insights Web Test and Alerts

In Application Insights kann beispielsweise unter "Availability" eine Ping-Prüfung auf eine Webseite hinterlegt werden, die zyklisch durchgeführt wird. Im Fehlerfall (lt. hinterlegten Bedingungen) wird ein Alert ausgeführt.

## Connect to and consume Azure services and third-party services

### Develop and App Service Logic App

#### create a Logic App

Nachdem dies alles im Azure Portal erstellt wird (außer man macht gerne ARM-Templates): [https://docs.microsoft.com/en-us/azure/logic-apps/quickstart-create-first-logic-app-workflow](https://docs.microsoft.com/en-us/azure/logic-apps/quickstart-create-first-logic-app-workflow).

Die Logic Apps sind grundsätzlich allerdings auch nur JSON-Dateien. Für EAI (= Enterprise Application Integration) und B2B steht es zusätzlich den "Enterprise Integration Pack", der zusätzlich B2B, EDI und XML Erweiterungen zur Behandlung von komplexen B2B Aufgaben.

Im Falle von Fehlern oder zur Diagnose stehen folgende Möglichkeiten zur Verfügung:
* Trigger history
* Run history
* Runtime debugging

#### create a custom connector for Logic Apps

Um einen benutzerdefinierten Connector zu erstellen, muss die API beschrieben werden, mit der eine Verbindung hergestellt werden soll. Falls eine Postman Collection oder OpenAPI zur Verfügung steht, kann diese direkt hinzugefügt werden.

#### create a custom template for Logic Apps

Es ist wahrscheinlich am sinnvollsten die Logic App im Portal zu erstellen und anschließend das ARM-Template zu speichern und zu editieren.

### Integrate Azure Search within solutions

Mein gesamtes Beispiel in .NET ist unter [https://github.com/stenet/az-203-prep/tree/master/vs/AzSearch](https://github.com/stenet/az-203-prep/tree/master/vs/AzSearch).

#### create an Azure Search index

Ich habe zwar versucht einen Search Service mit PowerShell oder der Azure CLI zu erstellen, aber in PowerShell gab es das Cmdlet dafür überhaupt nicht und die Azure CLI meinte immer nur, dass der Name bereits in Verwendung ist. Daher habe ihn dann im Portal erstellt.

Die Erstellung eines Index ist ähnlich der Erstellung einer Tabelle in einer relationalen Datenbank. Heißt es gibt einen Primärschlüssel, Spalten (= Felder) und Datentypen. Pro Feld kann definiert werden, ob

* retrievable
* filterable
* sortable
* searchable

Mit der .NET SDK geht's wie folgt:

```csharp
var serviceClient = new SearchServiceClient(
  SERVICE_NAME,
  new SearchCredentials(API_KEY));

if (serviceClient.Indexes.Exists(INDEX_NAME))
  serviceClient.Indexes.Delete(INDEX_NAME);

serviceClient.Indexes.Create(new Microsoft.Azure.Search.Models.Index()
{
  Name = INDEX_NAME,
  Fields = new List<Field>()
  {
    new Field("Id", DataType.String) { IsKey = true, IsRetrievable = true },
    new Field("Description", DataType.String) { IsSearchable = true, IsRetrievable = true }
  }
});
```

#### import searchable data

Daten können aus div. Quelle importiert werden:

* Azure SQL Database
* SQL Server auf einer Azure VM
* Cosmos DB
* Blob Storage
* Table Storage

Dabei können die Datensätze durch cognitive Funktionen erweitert werden: 

* Personennamen extrahieren
* Firmennamen extrahieren
* Orte extrahieren
* Übersetzung
* ...

Daten mit dem .NET SDK in den Index laden:

```csharp
var indexClient = serviceClient.Indexes.GetClient(INDEX_NAME);

var path = @"c:\temp";
var pattern = "*.txt";
var files = Directory.GetFiles(path, pattern);

var items = new List<object>();

foreach (var filePath in files)
{
  var text = File.ReadAllText(filePath);
  items.Add(new
  {
    Id = Convert.ToBase64String(Encoding.UTF8.GetBytes(filePath)),
    Description = text
  });
}

var batch = IndexBatch.Upload(items);
indexClient.Documents.Index(batch);
```

#### query the Azure Search index

Bei der Suche gibt es zwei Möglichkeiten des Abfragentyps:

* simple - unterstützt und, oder, Suffix, Phrase
* full  - Lucene Abfragesyntax

Beispiel für eine einfache Suche:

```
https://search20200203t2.search.windows.net/indexes/hotels-sample-index/docs?api-version=2019-05-06&%24top=10&search=kirkland
```

Bei der Suche muss immer die api-version angegeben werden. Wird kein expliziter queryType angegeben, handelt es sich um simple.

Daten mit der .NET SDK suchen:

```csharp
var searchClient = new SearchIndexClient(
  SERVICE_NAME,
  INDEX_NAME,
  new SearchCredentials(API_KEY));

var searchResult = searchClient.Documents.Search("peta");

var resultItem = searchResult.Results.FirstOrDefault();
if (resultItem == null)
  return;

var resultFilePath = Encoding.UTF8.GetString(Convert.FromBase64String((string)resultItem.Document["Id"]));
```

#### implement cognitive search

Bei import searchable data bereits erwähnt.

### Implement API management

#### establish API Gateways

TODO

#### create an APIM instance

```powershell
New-AzApiManagement `
  -ResourceGroupName TestRG `
  -Location "West Europe" `
  -Name apim20200203 `
  -Sku Developer `
  -Organization "Unbekannt" `
  -AdminEmail "email@domain.com"
```

Das erstellen dauert gefühlt zwei Stunden ... ;-)

#### configure authentication for APIs

TODO

#### define policies for APIs

Nachfolgend ein paar Policies, die öfter in der Dokumentation erwähnt werden:

* find-and-replace - inbound, outbound, backend; sucht nach einen Text im Request oder Response und ersetzt diesen durch einen anderen Text
* cache-lookup - inbound; liefert den Response aus dem Cache
* cache-lookup-value - inbound, outbound, backend; liefert einen Wert aus dem Cache
* cache-store - outbound, speichert den Response in den Cache
* cache-store-value - inbound, outbound, backend; speichert einen Wert in den Cache
* set-variable - inbound, outbound, backend; setzt, die der Name schon vermuten lässt, eine Variable ;-)

Für Berechtigungen gibt es folgende Policies:
* authentication-basic
* authentication-certificate
* authentication-managed-identity

### Develop event-based solutions

#### implement solutions that use Azure Event Grid

![Event Grid](images/az_event_grid.png)

[https://docs.microsoft.com/en-us/azure/event-grid/overview](https://docs.microsoft.com/en-us/azure/event-grid/overview)

Über Event Grid werden in Azure alle möglichen Events verteilt. Wie in der obigen Grafik ersichtlich, gibt es jede Menge Quellen, die Events an das Event Grid senden. Auf der anderen Seite gibt es Event Handler, die sich auf bestimmte Events registrieren und daraus Aktionen auslösen. Sprich, dem der ein Event abschickt ist es egal, wer oder ob überhaupt sich jemand darum kümmert.

So kann das Event Grid z.B. dafür verwendet werden, dass wenn ein Bild in einen Container im Storage Account geladen wird, dass dieses von einer Azure Function runterskaliert wird.

#### implement solutions that use Azure Notification Hubs

Thema wurde weiter oben bereits behandelt/angeschnitten.

#### implement solutions that use Azure Event Hub

Der Event Hub wird dann eingesetzt, wenn sehr viele Events gleichzeitig versendet und empfangen werden soll (er kann Millionen von Events gleichzeitig empfangen/versenden).

Mittels Event Hubs Capture können Events in einen Azure Blob Storage oder Azure Data Lake Storage gestreamt werden (mit Angabe eines Partition-Keys).

### Develop message-based solutions

#### implement solutions that use Azure Service Bus

Um einen Service Bus zu erstellen:

```powershell
New-AzServiceBusNamespace `
  -ResourceGroupName TestRG `
  -Name bus20200205 `
  -Location "West Europe" `
  -SkuName Standard
```

Service Bus eignet sich wunderbar für die Kommunikation über Prozess- und Hostgrenzen hinweg.

Unterscheidung zwischen Queue und Topic:

* Queue - pro Nachricht ein Empfänger
* Topic - pro Nachricht mehrere Empfänger (Subscriptions)

Beispiel für die Verwendung einer Queue

```csharp
var queueClient = new QueueClient(
  CONNECTION_STRING, 
  QUEUE_NAME);

var messageHandlerOptions = new MessageHandlerOptions(OnExceptionReceived)
{
  MaxConcurrentCalls = 1,
  AutoComplete = false
};
queueClient.RegisterMessageHandler(OnMessageReveived, messageHandlerOptions);

var messageBytes = Encoding.UTF8.GetBytes("Nachricht");
queueClient.SendAsync(new Message(messageBytes));
```

Bei der Verwendung eines Topics funktioniert es leicht anders. Dort wird ein TopicClient und eine SubscriptionClient benötigt; der TopicClient versendet, der SubscriptionClient empfängt. 

Ein komplettes ist unter [https://github.com/stenet/az-203-prep/tree/master/vs/AzServiceBus](https://github.com/stenet/az-203-prep/tree/master/vs/AzServiceBus).

Dem SubscriptionClient können "Rules" hinzugefügt werden. Diese schränken die Nachrichten, die empfangen werden sollen ein (z.B. nur Nachrichten mit Priorität hoch). Es gibt folgende Filter:

* Boolean Filter - ein true/false Filter
* SQL Filter - SQL ähnlicher Filter [https://docs.microsoft.com/en-us/azure/service-bus-messaging/service-bus-messaging-sql-filter](https://docs.microsoft.com/en-us/azure/service-bus-messaging/service-bus-messaging-sql-filter)
* Correlation Filter - unterstützt nur "gleich" Operator; darin aber effizienter als SQL Filter

Zu berücksichtigen gilt, dass die Filter alle Nachrichten-Eigenschaften evaluieren können, nicht aber den Body der Nachricht! Dafür können aber bei UserProperties hinzugefügt werden, die ebenfalls validiert werden können.

#### implement solutions that use Azure Queue Storage queues

In einer sehr vereinfachten Form kann das, was mit dem Service Bus und Queue gemacht werden kann, auch mit der Queue Storage gemacht werden. Der Nachteil ist allerdings, dass hier ein ständiges Pollen auf neue Nachrichten gemacht werden muss ...

```csharp
static void Main(string[] args)
{
  var storageAccount = CloudStorageAccount.Parse(CONNECTION_STRING);
  var queueClient = storageAccount.CreateCloudQueueClient();
  var queue = queueClient.GetQueueReference("testqueue");

  ReceiveMessageAsync(queue);

  var random = new Random();
  while (true)
  {
    Console.WriteLine("Press enter to send a message");
    Console.ReadLine();

    SendMessageAsync(queue, $"Random {random.Next(0, 1000)}").Wait();
  }
}

static async void ReceiveMessageAsync(CloudQueue queue)
{
  while (true)
  {
    var message = await queue.GetMessageAsync();

    if (message != null)
    {
      Console.WriteLine($"received: {message.AsString}");
      await queue.DeleteMessageAsync(message);
    }
  }
}
static async Task SendMessageAsync(CloudQueue queue, string message)
{
  await queue.AddMessageAsync(new CloudQueueMessage(message));
}
```

Dieses Beispiel ist [https://github.com/stenet/az-203-prep/tree/master/vs/AzStorageQueue](https://github.com/stenet/az-203-prep/tree/master/vs/AzStorageQueue).