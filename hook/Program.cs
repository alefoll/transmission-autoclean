using System.IO.Pipes;

const string SONARR_DOWNLOAD_CLIENT_ENV = "sonarr_download_client";
const string SONARR_DOWNLOAD_ID_ENV = "sonarr_download_id";

const string RADARR_DOWNLOAD_CLIENT_ENV = "radarr_download_client";
const string RADARR_DOWNLOAD_ID_ENV = "radarr_download_id";

var environmentVariables = Environment.GetEnvironmentVariables();

var sonarr_download_client = environmentVariables[SONARR_DOWNLOAD_CLIENT_ENV] as string;
var sonarr_download_id = environmentVariables[SONARR_DOWNLOAD_ID_ENV] as string;

var radarr_download_client = environmentVariables[RADARR_DOWNLOAD_CLIENT_ENV] as string;
var radarr_download_id = environmentVariables[RADARR_DOWNLOAD_ID_ENV] as string;

if (sonarr_download_client != null
    && sonarr_download_client == "Transmission"
    && sonarr_download_id != null) {
    sendHashToDeleteToService(sonarr_download_id.ToUpper());
}

if (radarr_download_client != null
    && radarr_download_client == "Transmission"
    && radarr_download_id != null) {
    sendHashToDeleteToService(radarr_download_id.ToUpper());
}

foreach(var hash in args) {
    sendHashToDeleteToService(hash.ToUpper());
}

static void sendHashToDeleteToService(string hash) {
    var pipeClient = new NamedPipeClientStream(".", "transmission-autoclean", PipeDirection.Out);

    pipeClient.Connect(5000); // 5 sec

    using (var streamWriter = new StreamWriter(pipeClient)) {
        streamWriter.WriteLine(hash);
    };

    pipeClient.Close();
}