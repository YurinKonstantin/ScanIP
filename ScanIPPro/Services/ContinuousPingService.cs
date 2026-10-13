using ScanIPPro.Models;
using System;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

namespace ScanIPPro.Services
{
    public class ContinuousPingService : IDisposable
    {
        private CancellationTokenSource _cts;
        private string _target;

        public event Action<PingSample> SampleReceived;

        public bool IsRunning => _cts != null && !_cts.IsCancellationRequested;

        public void Start(string target)
        {
            Stop();
            _target = target;
            _cts = new CancellationTokenSource();
            _ = RunAsync(_cts.Token);
        }

        public void Stop()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

        private async Task RunAsync(CancellationToken token)
        {
            using var ping = new Ping();

            while (!token.IsCancellationRequested)
            {
                try
                {
                    var reply = await ping.SendPingAsync(_target, 2000);
                    SampleReceived?.Invoke(new PingSample
                    {
                        Timestamp = DateTime.Now,
                        RoundtripTime = reply.RoundtripTime,
                        Success = reply.Status == IPStatus.Success,
                        Status = reply.Status.ToString()
                    });
                }
                catch (Exception ex)
                {
                    SampleReceived?.Invoke(new PingSample
                    {
                        Timestamp = DateTime.Now,
                        Success = false,
                        Status = ex.Message
                    });
                }

                try
                {
                    await Task.Delay(1000, token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
