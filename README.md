# RabbitMQ.Fakes
RabbitMQ.Fakes is a library that contains fake implementations of the RabbitMQ.Client interfaces.  These are intended to be used for testing so that unit tests who depend on RabbitMQ can be executed fully in memory withouth the dependence on an external RabbitMQ server.

# Requirements
* .NET 4.5
* Nuget Package Manger

# Projects
* __RabbitMQ.Fakes:__ Implementation of the fakes
* __RabbitMQ.Fakes.Tests:__ Unit tests around the fake implementation

# Fakes
* __RabbitServer:__ In memory representation of a Rabbit server.  This is where the Exchanges / Queues / Bindings / Messages are held.
* __FakeConnectionFactory:__ Fake implementation of the RabbitMQ ConnectionFactory.  Returns a FakeConnection when the .CreateConnection() method is called
* __FakeConnection:__ Fake implementation of the RabbitMQ IConnection.  Returns a FakeModel when the .CreateModel() method is called
* __FakeModel:__ Fake implementation of the RabbitMQ IModel.  Interacts with the RabbitServer instance passed into the FakeConnectionFactory.

# Sample Usage
See the UseCases in the RabbitMQ.Fakes.Tests project for sample usage
