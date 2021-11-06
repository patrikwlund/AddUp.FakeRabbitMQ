
// This file is used by Code Analysis to maintain SuppressMessage 
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given 
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Critical Code Smell", "S1186:Methods should not be empty", Justification = "Fake implementations")]
[assembly: SuppressMessage("Major Code Smell", "S4144:Methods should not have identical implementations", Justification = "<Pending>", Scope = "member", Target = "~M:AddUp.RabbitMQ.Fakes.FakeModel.Abort(System.UInt16,System.String)")]
[assembly: SuppressMessage("Major Code Smell", "S1854:Unused assignments should be removed", Justification = "False positive", Scope = "member", Target = "~M:AddUp.RabbitMQ.Fakes.FakeModel.BasicPublish(System.String,System.String,System.Boolean,System.Boolean,RabbitMQ.Client.IBasicProperties,System.Byte[])")]
[assembly: SuppressMessage("Minor Code Smell", "S1116:Empty statements should be removed", Justification = "<Pending>", Scope = "member", Target = "~M:AddUp.RabbitMQ.Fakes.RabbitQueue.ClearMessages")]
