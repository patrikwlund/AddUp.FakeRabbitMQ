using System.Collections.Generic;
using RabbitMQ.Client;

namespace AddUp.RabbitMQ.Fakes
{
    // Adapted from RabbitMQ.Client.Impl.BasicProperties and RabbitMQ.Client.Framing.BasicProperties
    internal sealed class FakeBasicProperties : IBasicProperties
    {
        private bool appIdPresent;
        private string appId;
        public string AppId
        {
            get => appId;
            set
            {
                appIdPresent = value != null;
                appId = value;
            }
        }

        private bool clusterIdPresent;
        private string clusterId;
        public string ClusterId
        {
            get => clusterId;
            set
            {
                clusterIdPresent = value != null;
                clusterId = value;
            }
        }

        private bool contentEncodingPresent;
        private string contentEncoding;
        public string ContentEncoding
        {
            get => contentEncoding;
            set
            {
                contentEncodingPresent = value != null;
                contentEncoding = value;
            }
        }

        private bool contentTypePresent;
        private string contentType;
        public string ContentType
        {
            get => contentType;
            set
            {
                contentTypePresent = value != null;
                contentType = value;
            }
        }

        private bool correlationIdPresent;
        private string correlationId;
        public string CorrelationId
        {
            get => correlationId;
            set
            {
                correlationIdPresent = value != null;
                correlationId = value;
            }
        }

        private bool deliveryModePresent;
        private byte deliveryMode;
        public byte DeliveryMode
        {
            get => deliveryMode;
            set
            {
                deliveryModePresent = true;
                deliveryMode = value;
            }
        }

        private bool expirationPresent;
        private string expiration;
        public string Expiration
        {
            get => expiration;
            set
            {
                expirationPresent = value != null;
                expiration = value;
            }
        }

        private bool headersPresent;
        private IDictionary<string, object> headers;
        public IDictionary<string, object> Headers
        {
            get => headers;
            set
            {
                headersPresent = value != null;
                headers = value;
            }
        }

        private bool messageIdPresent;
        private string messageId;
        public string MessageId
        {
            get => messageId;
            set
            {
                messageIdPresent = value != null;
                messageId = value;
            }
        }

        public bool Persistent
        {
            get => DeliveryMode == 2;
            set => DeliveryMode = (byte)(value ? 2 : 1);
        }

        private bool priorityPresent;
        private byte priority;
        public byte Priority
        {
            get => priority;
            set
            {
                priorityPresent = true;
                priority = value;
            }
        }

        private bool replyToPresent;
        private string replyTo;
        public string ReplyTo
        {
            get => replyTo;
            set
            {
                replyToPresent = value != null;
                replyTo = value;
            }
        }

        public PublicationAddress ReplyToAddress
        {
            get
            {
                _ = PublicationAddress.TryParse(ReplyTo, out var result);
                return result;
            }
            set => ReplyTo = value.ToString();
        }

        private bool timestampPresent;
        private AmqpTimestamp timestamp;
        public AmqpTimestamp Timestamp
        {
            get => timestamp;
            set
            {
                timestampPresent = true;
                timestamp = value;
            }
        }

        private bool typePresent;
        private string type;
        public string Type
        {
            get => type;
            set
            {
                typePresent = value != null;
                type = value;
            }
        }

        private bool userIdPresent;
        private string userId;
        public string UserId
        {
            get => userId;
            set
            {
                userIdPresent = value != null;
                userId = value;
            }
        }

        public ushort ProtocolClassId => 60;
        public string ProtocolClassName => "basic";

        public void ClearAppId() => appIdPresent = false;
        public void ClearClusterId() => clusterIdPresent = false;
        public void ClearContentEncoding() => contentEncodingPresent = false;
        public void ClearContentType() => contentTypePresent = false;
        public void ClearCorrelationId() => correlationIdPresent = false;
        public void ClearDeliveryMode() => deliveryModePresent = false;
        public void ClearExpiration() => expirationPresent = false;
        public void ClearHeaders() => headersPresent = false;
        public void ClearMessageId() => messageIdPresent = false;
        public void ClearPriority() => priorityPresent = false;
        public void ClearReplyTo() => replyToPresent = false;
        public void ClearTimestamp() => timestampPresent = false;
        public void ClearType() => typePresent = false;
        public void ClearUserId() => userIdPresent = false;

        public bool IsAppIdPresent() => appIdPresent;
        public bool IsClusterIdPresent() => clusterIdPresent;
        public bool IsContentEncodingPresent() => contentEncodingPresent;
        public bool IsContentTypePresent() => contentTypePresent;
        public bool IsCorrelationIdPresent() => correlationIdPresent;
        public bool IsDeliveryModePresent() => deliveryModePresent;
        public bool IsExpirationPresent() => expirationPresent;
        public bool IsHeadersPresent() => headersPresent;
        public bool IsMessageIdPresent() => messageIdPresent;
        public bool IsPriorityPresent() => priorityPresent;
        public bool IsReplyToPresent() => replyToPresent;
        public bool IsTimestampPresent() => timestampPresent;
        public bool IsTypePresent() => typePresent;
        public bool IsUserIdPresent() => userIdPresent;
    }
}
