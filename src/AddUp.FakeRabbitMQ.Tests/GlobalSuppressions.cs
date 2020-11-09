// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Style", "IDE0058:Expression value is never used", Justification = "There are a lot of these ones in Unit Tests")]
[assembly: SuppressMessage("Style", "IDE0018:Inline variable declaration", Justification = "<Pending>", Scope = "member", Target = "~M:RabbitMQ.Fakes.Tests.UseCases.ReceiveMessages.QueueingConsumer_MessagesSentAfterConsumerIsCreated_ReceiveMessagesOnQueue")]
[assembly: SuppressMessage("Style", "IDE0018:Inline variable declaration", Justification = "<Pending>", Scope = "member", Target = "~M:RabbitMQ.Fakes.Tests.UseCases.ReceiveMessages.QueueingConsumer_MessagesOnQueueBeforeConsumerIsCreated_ReceiveMessagesOnQueue")]

