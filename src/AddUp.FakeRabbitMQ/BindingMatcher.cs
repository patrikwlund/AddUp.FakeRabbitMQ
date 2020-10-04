using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using RabbitMQ.Client;

namespace AddUp.RabbitMQ.Fakes
{
    internal static class BindingMatcherFactory
    {
        public static IBindingMatcher Create(string exchangeType)
        {
            switch (exchangeType?.ToLowerInvariant() ?? "")
            {
                case ExchangeType.Direct: return new DirectBindingMatcher();
                case ExchangeType.Fanout: return new FanoutBindingMatcher();
                case ExchangeType.Topic: return new TopicBindingMatcher();
                case ExchangeType.Headers: return new HeadersBindingMatcher();
                default: return new DefaultBindingMatcher();
            }
        }
    }

    internal interface IBindingMatcher
    {
        bool Matches(string routingKey, string bindingKey);
    }

    internal class DefaultBindingMatcher : IBindingMatcher
    {
        public bool Matches(string routingKey, string bindingKey) => string.IsNullOrWhiteSpace(routingKey);
    }

    internal class DirectBindingMatcher : IBindingMatcher
    {
        public bool Matches(string routingKey, string bindingKey) => routingKey == bindingKey;
    }

    internal class FanoutBindingMatcher : IBindingMatcher
    {
        public bool Matches(string routingKey, string bindingKey) => true;
    }

    internal class TopicBindingMatcher : IBindingMatcher
    {
        private static readonly ConcurrentDictionary<string, Regex> cache = new ConcurrentDictionary<string, Regex>();

        public bool Matches(string routingKey, string bindingKey)
        {
            if (routingKey == bindingKey) return true;
            var regex = ConvertBindingKey(bindingKey);
            return regex.IsMatch(routingKey);
        }

        // See https://stackoverflow.com/questions/50679145/how-to-match-the-routing-key-with-binding-pattern-for-rabbitmq-topic-exchange-us
        private Regex ConvertBindingKey(string key)
        {
            if (!cache.ContainsKey(key))
            {
                const string word = "[^.]+";

                var pattern = key;
                pattern = new Regex("#(?:\\.#)+").Replace(pattern, "#"); // Replace duplicate # (this makes things simpler)
                pattern = new Regex("\\*").Replace(pattern, word); // Replace *

                // replace #

                // lone #
                if (pattern == "#") return new Regex(
                    "(?:" + word + "(?:\\." + word + ")*)?", RegexOptions.Compiled);

                pattern = new Regex("#(?:\\.#)+").Replace(pattern, "(?:" + word + "\\.)*", 1);
                pattern = new Regex("\\.#").Replace(pattern, "(?:\\." + word + ")*", 1);
                pattern = new Regex("#\\.").Replace(pattern, "(?:" + word + "\\.)*", 1); // This one is when the key starts with #.

                var regex = new Regex("^" + pattern + "$");
                _ = cache.TryAdd(key, regex);
            }

            return cache[key];
        }
    }

    internal class HeadersBindingMatcher : IBindingMatcher
    {
        // This is really not a Headers matcher...
        public bool Matches(string routingKey, string bindingKey) => true;
    }
}
