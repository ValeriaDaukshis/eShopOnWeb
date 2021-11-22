using Ardalis.GuardClauses;
using BlazorShared;
using Microsoft.Azure.ServiceBus;
using Microsoft.eShopWeb.ApplicationCore.Entities;
using Microsoft.eShopWeb.ApplicationCore.Entities.BasketAggregate;
using Microsoft.eShopWeb.ApplicationCore.Entities.OrderAggregate;
using Microsoft.eShopWeb.ApplicationCore.Interfaces;
using Microsoft.eShopWeb.ApplicationCore.Specifications;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.eShopWeb.ApplicationCore.Services
{
    public class OrderService : IOrderService
    {
        private readonly IAsyncRepository<Order> _orderRepository;
        private readonly IUriComposer _uriComposer;
        private readonly IAsyncRepository<Basket> _basketRepository;
        private readonly IAsyncRepository<CatalogItem> _itemRepository;
        private readonly string _orderProcessorTriggerUrl;
        private readonly ServiceBusConfig _serviceBusConfig;
        static IQueueClient queueClient;

        public OrderService(IAsyncRepository<Basket> basketRepository,
            IAsyncRepository<CatalogItem> itemRepository,
            IAsyncRepository<Order> orderRepository,
            IUriComposer uriComposer,
            BaseUrlConfiguration baseUrlConfiguration,
            ServiceBusConfig serviceBusConfig)
        {
            _orderRepository = orderRepository;
            _uriComposer = uriComposer;
            _basketRepository = basketRepository;
            _itemRepository = itemRepository;
            _orderProcessorTriggerUrl = baseUrlConfiguration.OrderProcessorTriggerBase;
            _serviceBusConfig = serviceBusConfig;
        }

        public async Task CreateOrderAsync(int basketId, Address shippingAddress)
        {
            var basketSpec = new BasketWithItemsSpecification(basketId);
            var basket = await _basketRepository.FirstOrDefaultAsync(basketSpec);

            Guard.Against.NullBasket(basketId, basket);
            Guard.Against.EmptyBasketOnCheckout(basket.Items);

            var catalogItemsSpecification = new CatalogItemsSpecification(basket.Items.Select(item => item.CatalogItemId).ToArray());
            var catalogItems = await _itemRepository.ListAsync(catalogItemsSpecification);

            var items = basket.Items.Select(basketItem =>
            {
                var catalogItem = catalogItems.First(c => c.Id == basketItem.CatalogItemId);
                var itemOrdered = new CatalogItemOrdered(catalogItem.Id, catalogItem.Name, _uriComposer.ComposePicUri(catalogItem.PictureUri));
                var orderItem = new OrderItem(itemOrdered, basketItem.UnitPrice, basketItem.Quantity);
                return orderItem;
            }).ToList();

            var order = new Order(basket.BuyerId, shippingAddress, items);
            await _orderRepository.AddAsync(order);

            await TriggerReserverFunction(order);
            await TriggerProcessorFunction(order);
        }

        private async Task TriggerReserverFunction(Order order)
        {
            queueClient = new QueueClient(_serviceBusConfig.ConnectionString, _serviceBusConfig.QueueName);

            var serializedOrder = JsonConvert.SerializeObject(order, Formatting.Indented);
            var message = new Message(Encoding.UTF8.GetBytes(serializedOrder));

            await queueClient.SendAsync(message);
            await queueClient.CloseAsync();
        }

        private async Task TriggerProcessorFunction(Order order)
        {
            var client = new HttpClient();
            var serializedOrder = JsonConvert.SerializeObject(order, Formatting.Indented);

            await client.PostAsync(
                _orderProcessorTriggerUrl,
                new StringContent(serializedOrder, Encoding.UTF8, "application/json"));
        }
    }
}
