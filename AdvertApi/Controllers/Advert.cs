using System;
using AdvertApi.Models;
using AdvertApi.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Collections.Generic;
using Amazon.SimpleNotificationService;
using Microsoft.Extensions.Configuration;
using AdvertApi.Models.Messages;
using Newtonsoft.Json;

namespace AdvertApi.Controllers
{
    [Route("adverts/v1")]
    [ApiController]
    public class Advert : ControllerBase
    {
        private readonly IConfiguration _configuration;

        private readonly IAdvertStorageService _advertStorageService;
        public Advert(IAdvertStorageService advertStorageService, IConfiguration configuration)
        {
            _advertStorageService = advertStorageService;
            _configuration = configuration;
        }


        [HttpGet]
        [Route("Create")]
        [ProducesResponseType(404)]
        [ProducesResponseType(201, Type =typeof(CreateAdvertResponse))]
        public async Task<IActionResult> Create(AdvertModel model)
        {
            string recordId;
            try
            {
                recordId = await _advertStorageService.Add(model);
                
            }
            catch (KeyNotFoundException)
            {
                return new NotFoundResult();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
               
            }

            return StatusCode(201, new CreateAdvertResponse{ Id = recordId});
        }

        //--------------------------------------------------------------------------------------------------------------------------------
        [HttpPut]
        [Route("Confirm")]
        [ProducesResponseType(404)]
        [ProducesResponseType(200, Type = typeof(CreateAdvertResponse))]
        public async Task<IActionResult> Confirm(ConfirmAdvertModel model)
        {
            try
            {
                await _advertStorageService.ConfirmAsync(model);
                await RaiseAdvertConfirmMessage(model);
            }
            catch (KeyNotFoundException)
            {
                return new NotFoundResult();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }

            return new OkResult();



        }

        private async Task RaiseAdvertConfirmMessage(ConfirmAdvertModel model)
        {
            var topicArn = _configuration.GetValue<string>("TopicArn"); //Pega a informação do appsetings json
            var dbModel = await _advertStorageService.GetByIdAsync(model.Id);

            using (var client = new AmazonSimpleNotificationServiceClient())
            {
                var message = new AdvertConfirmedMessage{
                    Id = model.Id,
                    Title = dbModel.Title,

                };
                var messageJson = JsonConvert.SerializeObject(message);
                await client.PublishAsync(topicArn,messageJson);
            }
        }
    }

}
