using System.Diagnostics.CodeAnalysis;
using System.IO.Pipes;
using Transmission.API.RPC;
using Transmission.API.RPC.Entity;

namespace service {
    public class Worker : BackgroundService {
        private readonly ILogger<Worker> _logger;

        private List<string> tmpHash = new List<string>();

        private const int DELAY_BEFORE_DELETE_TORRENT_IN_MINUTES = 15;

        public Worker(ILogger<Worker> logger) {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            LogInformation("Service Started");

            while (!stoppingToken.IsCancellationRequested) {
                using (var pipeServer = new NamedPipeServerStream("transmission-autoclean", PipeDirection.In)) {
                    LogInformation("Wait for a new client");

                    await pipeServer.WaitForConnectionAsync(stoppingToken);

                    LogInformation("Connected");

                    var streamReader = new StreamReader(pipeServer);

                    var message = await streamReader.ReadLineAsync(stoppingToken);

                    if (message == null) {
                        pipeServer.Disconnect();

                        continue;
                    }

                    LogInformation($"New hash: { message }");

                    pipeServer.Disconnect();

                    LogInformation($"Goodbye client!");

                    _ = DeleteTorrentWithHash(message);
                }
            }
        }

        private async Task DeleteTorrentWithHash(string hashToDelete) {
            if (tmpHash.Contains(hashToDelete)) {
                LogInformation($"Hash already marked to deletion: { hashToDelete }");

                return;
            }

            tmpHash.Add(hashToDelete);

            LogInformation($"Delete torrent with hash: { hashToDelete }");

            var client = new Client("http://127.0.0.1:9091/transmission/rpc", null, "", "");

            if (GetTorrentFromHash(client, hashToDelete) == null) {
                LogInformation($"Torrent not found: { hashToDelete }");

                return;
            }

            await Task.Delay(TimeSpan.FromMinutes(DELAY_BEFORE_DELETE_TORRENT_IN_MINUTES));

            var torrentToDelete = GetTorrentFromHash(client, hashToDelete);

            if (torrentToDelete == null) {
                LogInformation($"Torrent not found: {hashToDelete}");

                return;
            }

            if (torrentToDelete.PercentDone < 1) {
                LogInformation($"Torrent in progress, retry: { hashToDelete }");

                _ = DeleteTorrentWithHash(hashToDelete);

                return;
            }

            client.TorrentRemove([torrentToDelete.ID], true);

            LogInformation($"Torrent deleted: { hashToDelete }");

            tmpHash.Remove(hashToDelete);
        }

        [return: MaybeNull]
        private TorrentInfo GetTorrentFromHash(Client client, string hash) {
            var allTorrents = client.TorrentGet(TorrentFields.ALL_FIELDS);

            return allTorrents.Torrents
                .FirstOrDefault(torrent => torrent.HashString.ToUpper() == hash.ToUpper());
        }

        private void LogInformation(string message) {
            if (_logger.IsEnabled(LogLevel.Information)) {
                _logger.LogInformation(message);
            }
        }
    }
}
