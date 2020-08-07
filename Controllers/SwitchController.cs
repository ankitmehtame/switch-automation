using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WemoSwitchAutomation.Models;
using WemoSwitchAutomation.Resources;

namespace WemoSwitchAutomation.Controllers
{
    [Route("[controller]")]
    [Route("api/[controller]")]
    public class SwitchController : ControllerBase
    {
        private ILogger Logger { get; }
        protected IConfiguration Configuration { get; }
        protected IHttpClientFactory HttpClientFactory { get; }

        public SwitchController(ILoggerFactory loggerFactory, IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            Logger = loggerFactory.CreateLogger<SwitchController>();
            Configuration = configuration;
            HttpClientFactory = httpClientFactory;
        }

        [HttpPost]
        [Route("on/{id}")]
        public async Task TurnOn([FromRoute] string id)
        {
            try
            {
                var switchName = id;
                Logger.LogDebug($"Request to turn switch {switchName} on");
                var @switch = await GetCorrectSwitchIp(switchName);
                if (@switch.State)
                {
                    Logger.LogInformation($"Switch {switchName} is already on at IP {@switch.IP}");
                    return;
                }
                var request = SendSwitchOnOffRequest(@switch.IP, AllResources.SwitchOnRequestContent);
                await ValidateResponse(@switch.IP, request, true);
                Logger.LogInformation($"Switch {switchName} turned on at IP {@switch.IP}");
            }
            catch (AggregateException ex)
            {
                Logger.LogError($"Error in TurnOff where id = {id}, {ex.Message}\r\n{ex.StackTrace}. ");
                throw;
            }
            catch(Exception ex)
            {
                Logger.LogError($"Error in TurnOff where id = {id}, {ex.Message}\r\n{ex.StackTrace}. ");
                throw;
            }
        }

        [HttpPost]
        [Route("off/{id}")]
        public async Task TurnOff([FromRoute] string id)
        {
            try
            {
                var switchName = id;
                Logger.LogDebug($"Request to turn switch {switchName} off");
                var @switch = await GetCorrectSwitchIp(switchName);
                if (!@switch.State)
                {
                    Logger.LogInformation($"Switch {switchName} is already off at IP {@switch.IP}");
                    return;
                }
                var request = SendSwitchOnOffRequest(@switch.IP, AllResources.SwitchOffRequestContent);
                await ValidateResponse(@switch.IP, request, false);
                Logger.LogInformation($"Switch {switchName} turned off at IP {@switch.IP}");
            }
            catch (AggregateException ex)
            {
                Logger.LogError($"Error in TurnOff where id = {id}, {ex.Message}\r\n{ex.StackTrace}. ");
                throw;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error in TurnOff where id = {id}, {ex.Message}\r\n{ex.StackTrace}. ");
                throw;
            }
        }

        [HttpPost]
        [Route("toggle/{id}")]
        public async Task Toggle([FromRoute] string id)
        {
            try
            {
                var switchName = id;
                Logger.LogDebug($"Request to toggle switch {0}", switchName);
                var @switch = await GetCorrectSwitchIp(switchName);
                if (@switch.State)
                {
                    await SwitchOff(@switch.IP);
                    Logger.LogInformation($"Switch {switchName} toggled and turned off at IP {@switch.IP}");
                }
                else
                {
                    await SwitchOn(@switch.IP);
                    Logger.LogInformation($"Switch {switchName} toggled and turned on at IP {@switch.IP}");
                }
            }
            catch (AggregateException ex)
            {
                Logger.LogError($"Error in Toggle where id = {id}, {ex.Message}\r\n{ex.StackTrace}. ");
                throw;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error in Toggle where id = {id}, {ex.Message}\r\n{ex.StackTrace}. ");
                throw;
            }
        }

        [HttpGet]
        [Route("{id}")]
        public async Task<string> GetState([FromRoute] string id)
        {
            try
            {
                var switchName = id;
                Logger.LogDebug($"Request to get state of switch {switchName}");
                var @switch = await GetCorrectSwitchIp(switchName);
                var ret = @switch.State ? "On" : "Off";
                Logger.LogDebug($"Switch {switchName} state found as {ret} at IP {@switch.IP}");
                return ret;
            }
            catch (AggregateException ex)
            {
                Logger.LogError($"Error in GetState where id = {id}, {ex.Message}\r\n{ex.StackTrace}. ");
                throw;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error in GetState where id = {id}, {ex.Message}\r\n{ex.StackTrace}. ");
                throw;
            }
        }

        private async Task SwitchOn(string ip)
        {
            var request = SendSwitchOnOffRequest(ip, AllResources.SwitchOnRequestContent);
            await ValidateResponse(ip, request, true);
        }

        private async Task SwitchOff(string ip)
        {
            var request = SendSwitchOnOffRequest(ip, AllResources.SwitchOffRequestContent);
            await ValidateResponse(ip, request, false);
        }

        private static Task<HttpResponseMessage> SendSwitchOnOffRequest(string switchIp, string httpContent)
        {
            var url = $"http://{switchIp}/upnp/control/basicevent1";
            var httpClient = new HttpClient();
            var content = new StringContent(httpContent, Encoding.UTF8, "text/xml");
            content.Headers.Add("SOAPACTION", "\"urn:Belkin:service:basicevent:1#SetBinaryState\"");
            var httpRequest = httpClient.PostAsync(url, content);
            return httpRequest;
        }

        private static Task<HttpResponseMessage> SendGetSwitchStateRequest(string switchIp)
        {
            var url = $"http://{switchIp}/upnp/control/basicevent1";
            var httpClient = new HttpClient();
            var content = new StringContent(AllResources.GetSwitchStateRequestContent, Encoding.UTF8, "text/xml");
            content.Headers.Add("SOAPACTION", "\"urn:Belkin:service:basicevent:1#GetBinaryState\"");
            var httpRequest = httpClient.PostAsync(url, content);
            return httpRequest;
        }

        private async Task ValidateResponse(string ip, Task<HttpResponseMessage> request, bool expectedState)
        {
            var response = await request;
            var responseContent = await response.Content.ReadAsByteArrayAsync();
            var switchInfo = await GetSwitchStateWithIp(ip);
            if (switchInfo == null || switchInfo.State != expectedState)
                throw new InvalidOperationException($"Switch state was not as expected. Expected {expectedState}, was {(switchInfo == null ? "null" : switchInfo.State.ToString())}.");
        }

        private static string GetSwitchStateFromResponse(byte[] responseContent)
        {
            var responseText = Encoding.UTF8.GetString(responseContent);
            if (responseText == null)
            {
                return null;
            }
            var xdoc = XDocument.Parse(responseText);
            var binState = xdoc.Descendants().FirstOrDefault(n => n.Name.LocalName == "BinaryState");
            if (binState == null)
            {
                return null;
            }
            var statesText = binState.Value;
            if (statesText == null)
            {
                return null;
            }
            var states = statesText.Split(new [] { '|' }, StringSplitOptions.None);
            var state = states[0];
            return state;
        }

        private string[] GetSwitchIps(string switchName)
        {
            var switchIp = Configuration.GetValue<string>($"switch:{switchName}");
            return switchIp.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToArray();
        }

        private async Task<Switch> GetCorrectSwitchIp(string switchName)
        {
            var ips = GetSwitchIps(switchName);

            var tcsResult = new TaskCompletionSource<Switch>();
            var counter = ips.Length;

            foreach(var ip in ips)
            {
                var currentIp = ip;
                var t = GetSwitchStateWithIp(currentIp).ContinueWith(t => 
                    {
                        var curCount = Interlocked.Decrement(ref counter);
                        if(t.Status == TaskStatus.RanToCompletion && t.Result != null)
                        {
                            tcsResult.TrySetResult(t.Result);
                            return;
                        }
                        if (curCount == 0)
                            tcsResult.TrySetException(new InvalidOperationException("Unable to connect to switch"));
                    });
            }
            
            await tcsResult.Task;

            return tcsResult.Task.Result;
        }

        private async Task<Switch> GetSwitchStateWithIp(string ip)
        {
            return await await Task.Factory.StartNew(async () =>
                {
                    var httpClient = HttpClientFactory.CreateClient();
                    try
                    {
                        var reqTask = SendGetSwitchStateRequest(ip);
                        if (await Task.WhenAny(reqTask, Task.Delay(TimeSpan.FromSeconds(15))) == reqTask)
                        {
                            var response = await reqTask;
                            var responseContent = await response.Content.ReadAsByteArrayAsync();
                            var stateText = GetSwitchStateFromResponse(responseContent);
                            var state = stateText == "0" ? false : true;
                            return new Switch { IP = ip, State = state };
                        }
                        else
                        {
                            // timeout logic
                            return null;
                        }
                    }
                    catch (Exception)
                    {
                        // This is not the right IP.
                        return null;
                    }
                });
        }
    }
}
