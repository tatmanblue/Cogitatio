using System.Text;
using System.Text.Json;
using Cogitatio.Models;
using Microsoft.AspNetCore.Components;

namespace Cogitatio.Components.Pages;

public partial class Contact : ComponentBase
{
    [Inject]
    private ILogger<Contact> logger { get; set; }

    [SupplyParameterFromForm]
    private RequestContact contactData { get; set; } = new ();

    private bool showContactForm { get; set; } = true;
    
    public void SendContactRequest()
    {
       
        var handler = new HttpClientHandler();
        handler.ClientCertificateOptions = ClientCertificateOption.Manual;
        handler.ServerCertificateCustomValidationCallback = 
            (httpRequestMessage, cert, cetChain, policyErrors) =>
            {
                return true;
            };
        
        HttpClient client = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://services.tatmangames.com/svc/"),
        };
        


        StringContent json = new StringContent(JsonSerializer.Serialize(contactData),
            Encoding.UTF8,
            "application/json");
        
        Task<string> task = Task.Run(async () =>
        {
            using HttpResponseMessage response = await client.PostAsync(
                "contact",
                json);

            string jsonResponse = await response.Content.ReadAsStringAsync();
            return jsonResponse;
        });

        // Wait for the task to complete synchronously
        string result = task.Result;
        logger.LogInformation($"Results of call are: {result}");

        showContactForm = false;
        StateHasChanged();
    }
}