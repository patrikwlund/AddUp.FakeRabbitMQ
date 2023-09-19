// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Style", "IDE0058:Expression value is never used", Justification = "There are a lot of these ones in Unit Tests")]
[assembly: SuppressMessage("Style", "IDE0046:Convert to conditional expression", Justification = "<Pending>", Scope = "member", Target = "~M:AddUp.RabbitMQ.Fakes.Bus.CreateChannel~RabbitMQ.Client.IModel")]
[assembly: SuppressMessage("Minor Code Smell", "S3963:\"static\" fields should be initialized inline", Justification = "<Pending>", Scope = "member", Target = "~M:AddUp.RabbitMQ.Fakes.FakeBasicPropertiesTests.#cctor")]
[assembly: SuppressMessage("Major Code Smell", "S6561:Avoid using \"DateTime.Now\" for benchmarking or timing operations", Justification = "<Pending>", Scope = "member", Target = "~M:AddUp.RabbitMQ.Fakes.QueueingBasicConsumer.SharedQueue`1.Dequeue(System.Int32,`0@)~System.Boolean")]
