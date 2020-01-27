# AddUp.RabbitMQ.Fakes

[![codecov.io](https://codecov.io/github/AddUpSolutions/AddUp.RabbitMQ.Fakes/coverage.svg?branch=master)](https://codecov.io/github/AddUpSolutions/AddUp.RabbitMQ.Fakes?branch=master)
[![Build Status](https://dev.azure.com/addupsolutions/oss/_apis/build/status/addupsolutions.AddUp.RabbitMQ.Fakes?branchName=master)](https://dev.azure.com/addupsolutions/oss/_build/latest?definitionId=3&branchName=master)
[![NuGet](https://img.shields.io/nuget/v/AddUp.RabbitMQ.Fakes.svg)](https://www.nuget.org/packages/AddUp.RabbitMQ.Fakes/)

## About

**AddUp.RabbitMQ.Fakes** is a fork of <https://github.com/Parametric/RabbitMQ.Fakes>. Thanks to the folks over there for their work without which our own version would have probably never seen the light.

**AddUp.RabbitMQ.Fakes** provides fake implementations of the **RabbitMQ.Client** interfaces (see <https://www.nuget.org/packages/RabbitMQ.Client> for the nuget package and <https://github.com/rabbitmq/rabbitmq-dotnet-client> for its source code). They are intended to be used for testing so that unit tests that depend on RabbitMQ can be executed fully in memory withouth needing an external RabbitMQ server.

**AddUp.RabbitMQ.Fakes** builds on top of the original project:

* Targets .NET Standard 2.0
* Supports Default, Direct, Topic and Fanout exchange types.
  * NB: Headers exchange type is not supported; however, it won't throw (it is implemented the same way as the Fanout type)

## License

This work is provided under the terms of the [MIT License](LICENSE).
