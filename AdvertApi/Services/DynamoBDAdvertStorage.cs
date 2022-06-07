using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AdvertApi.Models;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using AutoMapper;
namespace AdvertApi.Services
{
    public class DynamoBDAdvertStorage : IAdvertStorageService
    {
        private readonly IMapper _mapper;
        private readonly AmazonDynamoDBClient _client;
        public DynamoBDAdvertStorage(IMapper mapper, AmazonDynamoDBClient client)
        {
            _mapper = mapper;
            _client = client;
        }


        public async Task<string> Add(AdvertModel model)
        {

            var dbModel = _mapper.Map<AdvertDbModel>(model);
            //Aqui completamos os itens que não foram mapeadoas 
            dbModel.Id = new Guid().ToString();
            dbModel.CreationDateTime = DateTime.UtcNow;
            dbModel.Status = AdvertStatus.Pending;

            using (var context = new DynamoDBContext(_client))
            {
                await context.SaveAsync(dbModel);

            }
            return dbModel.Id;
        }

        public async Task<string> AddAsync(AdvertModel model)
        {
            var dbModel = _mapper.Map<AdvertDbModel>(model);
            dbModel.Id = Guid.NewGuid().ToString();
            dbModel.CreationDateTime = DateTime.UtcNow;
            dbModel.Status = AdvertStatus.Pending;

            var table = _client.DescribeTableAsync("TableName");
            using (var context = new DynamoDBContext(_client))
            {
                await context.SaveAsync(dbModel);
            }
            return dbModel.Id;
        }
        public async Task<bool> CheckHealthAsync()
        {
            var tableData = await _client.DescribeTableAsync("nome-da-table");
            return string.Compare(tableData.Table.TableStatus, "active", true) == 0; //True or false
        }

        public async Task Confirm(ConfirmAdvertModel model)
        {

            using (var context = new DynamoDBContext(_client))
            {
                // 'grava" a ação feita
                var record = await context.LoadAsync<AdvertDbModel>(model.Id);
                // verifica se realmente existe 
                if (record == null)
                {
                    throw new KeyNotFoundException($"A record with ID ={model.Id} was not found");
                }

                //Verifica se já pode ser salvo
                if (model.Status == AdvertStatus.Active)
                {
                    record.Status = AdvertStatus.Active;
                    await context.SaveAsync(record);

                }

                //Caso não tenha status ativado indica que algo falhou
                //deleta do banco de dados
                else
                {
                    await context.DeleteAsync(record);
                }

            }
        }


        public async Task ConfirmAsync(ConfirmAdvertModel model)
        {

            using (var context = new DynamoDBContext(_client))
            {
                var record = await context.LoadAsync<AdvertDbModel>(model.Id);
                if (record == null) throw new KeyNotFoundException($"A record with ID={model.Id} was not found.");
                if (model.Status == AdvertStatus.Active)
                {
                    record.FilePath = model.FilePath;
                    record.Status = AdvertStatus.Active;
                    await context.SaveAsync(record);
                }
                else
                {
                    await context.DeleteAsync(record);
                }
            }

        }

        public async Task<List<AdvertModel>> GetAllAsync()
        {

            using (var context = new DynamoDBContext(_client))
            {
                var scanResult =
                    await context.ScanAsync<AdvertDbModel>(new List<ScanCondition>()).GetNextSetAsync();
                return scanResult.Select(item => _mapper.Map<AdvertModel>(item)).ToList();
            }

        }

        public async Task<AdvertModel> GetByIdAsync(string id)
        {

            using (var context = new DynamoDBContext(_client))
            {
                var dbModel = await context.LoadAsync<AdvertDbModel>(id);
                if (dbModel != null) return _mapper.Map<AdvertModel>(dbModel);
            }
            throw new KeyNotFoundException();
        }

           
    }
}
    



