using Microsoft.AspNetCore.Mvc;
using System; // Namespace for Console output
using System.Configuration; // Namespace for ConfigurationManager
using System.Threading.Tasks; // Namespace for Task
using Azure.Storage.Queues; // Namespace for Queue storage types
using Azure.Storage.Queues.Models; // Namespace for PeekedMessage
using ServiceQuenue.Classes;

namespace ServiceQuenue.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class AzureController : ControllerBase
    {
        private readonly ILogger<AzureController> _logger;
        private readonly IConfiguration Configuration;
        private readonly string connectionString;

        public AzureController(IConfiguration configRoot, ILogger<AzureController> logger)
        {
            _logger = logger;
            Configuration = configRoot;
            connectionString = Configuration.GetValue<string>("AzureConnectionString");
        }

        [HttpPost]
        public ActionResult CreateQueue([FromForm] string queueName)
        {
            try
            {
                // Instantiate a QueueClient which will be used to create and manipulate the queue
                QueueClient queueClient = new QueueClient(connectionString, queueName.ToLower());

                // Create the queue
                queueClient.CreateIfNotExists();

                if (queueClient.Exists())
                {                    
                    return Ok();
                }
                else
                {                    
                    return BadRequest("Make sure the Azurite storage emulator running and try again.");
                }
            }
            catch (Exception ex)
            {
                return BadRequest($"Exception: {ex.Message}\n\n");                
            }
        }
        [HttpPost]
        public ActionResult InsertMessage([FromForm] string queueName, [FromForm] string message)
        {
            // Instantiate a QueueClient which will be used to create and manipulate the queue
            QueueClient queueClient = new QueueClient(connectionString, queueName.ToLower());

            // Create the queue if it doesn't already exist
            queueClient.CreateIfNotExists();

            if (queueClient.Exists())
            {
                // Send a message to the queue
                queueClient.SendMessage(message);
            }

            return Ok();
        }
        [HttpPost]
        public ActionResult<ICollection<Message>> PeekMessage([FromForm] string queueName, [FromForm] int maxMessages=1)
        {
            QueueClient queueClient = new QueueClient(connectionString, queueName.ToLower());

            if (queueClient.Exists())
            {
                // Peek at the next message
                PeekedMessage[] peekedMessage = queueClient.PeekMessages(maxMessages);

                ICollection<Message> messages = peekedMessage
                    .Select(i => new Message { Id = i.MessageId, Body = i.Body.ToString() })
                    .ToList();
                // Display the message
                return Ok(messages);
            }
            return NotFound("Не найдена очередь");
        }
        [HttpPut]
        public ActionResult UpdateMessage([FromForm] string queueName, [FromForm] string messageId, [FromForm] string messageContent)
        {
            QueueClient queueClient = new QueueClient(connectionString, queueName.ToLower());

            if (queueClient.Exists())
            {
                // Get the message from the queue
                QueueMessage[] message = queueClient.ReceiveMessages();

                string PopReceipt = message.Where(i => i.MessageId == messageId).First().PopReceipt;

                // Update the message contents
                queueClient.UpdateMessage(messageId.ToString(),
                        PopReceipt,
                        messageContent,
                        TimeSpan.FromSeconds(60.0)  // Make it invisible for another 60 seconds
                    );
                return Ok();
            }
            return NotFound("Не найдена очередь");
        }
        [HttpDelete]
        public ActionResult DequeueMessage([FromForm] string queueName, [FromForm] string messageId)
        {
            // Instantiate a QueueClient which will be used to manipulate the queue
            QueueClient queueClient = new QueueClient(connectionString, queueName.ToLower());

            if (queueClient.Exists())
            {
                QueueProperties properties = queueClient.GetProperties();

                // Retrieve the cached approximate message count.
                int cachedMessagesCount = properties.ApproximateMessagesCount;

                // Get the next message
                QueueMessage[] retrievedMessage = queueClient.ReceiveMessages(cachedMessagesCount);

                string PopReceipt = retrievedMessage.Where(i => i.MessageId == messageId).First().PopReceipt;
                // Delete the message
                queueClient.DeleteMessage(messageId.ToString(), PopReceipt);

                return Ok();
            }
            return NotFound("Не найдена очередь");
        }
        [HttpGet]
        public ActionResult<int> GetQueueLength(string queueName)
        {
            QueueClient queueClient = new QueueClient(connectionString, queueName.ToLower());

            if (queueClient.Exists())
            {
                QueueProperties properties = queueClient.GetProperties();

                // Retrieve the cached approximate message count.
                int cachedMessagesCount = properties.ApproximateMessagesCount;

                // Display number of messages.
                return cachedMessagesCount;
            }
            return NotFound("Не найдена очередь");
        }
        [HttpDelete]
        public ActionResult DeleteQueue([FromForm] string queueName)
        {
            QueueClient queueClient = new QueueClient(connectionString, queueName.ToLower());

            if (queueClient.Exists())
            {
                // Delete the queue
                queueClient.Delete();
                return Ok();
            }
            return BadRequest();
        }

    }
}