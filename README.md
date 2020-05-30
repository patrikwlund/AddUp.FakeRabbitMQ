# AddUp.RabbitMQ.Fakes

![Build](https://github.com/addupsolutions/AddUp.RabbitMQ.Fakes/workflows/Build/badge.svg)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=addupsolutions_AddUp.RabbitMQ.Fakes&metric=alert_status)](https://sonarcloud.io/dashboard?id=addupsolutions_AddUp.RabbitMQ.Fakes)
[![Coverage](https://sonarcloud.io/api/project_badges/measure?project=addupsolutions_AddUp.RabbitMQ.Fakes&metric=coverage)](https://sonarcloud.io/dashboard?id=addupsolutions_AddUp.RabbitMQ.Fakes)
[![NuGet](https://img.shields.io/nuget/v/AddUp.RabbitMQ.Fakes.svg)](https://www.nuget.org/packages/AddUp.RabbitMQ.Fakes/)

## About

**AddUp.RabbitMQ.Fakes** is a fork of <https://github.com/Parametric/RabbitMQ.Fakes>. Thanks to the folks over there for their work without which our own version would have probably never seen the light.

**AddUp.RabbitMQ.Fakes** provides fake implementations of the **RabbitMQ.Client** interfaces (see <https://www.nuget.org/packages/RabbitMQ.Client> for the nuget package and <https://github.com/rabbitmq/rabbitmq-dotnet-client> for its source code). They are intended to be used for testing so that unit tests that depend on RabbitMQ can be executed fully in memory withouth needing an external RabbitMQ server.

**AddUp.RabbitMQ.Fakes** builds on top of the original project:

* Targets **.NET Standard 2.0**
* Supports Default, Direct, Topic and Fanout exchange types.
  * _NB: Headers exchange type is not supported; however, it won't throw (it is implemented the same way as the Fanout type)_

## History

### Version 1.2.0 - 2019/12/11

* First released version of **AddUp.RabbitMQ.Fakes**. Based on [RabbitMQ.Client version 5.1.2](https://www.nuget.org/packages/RabbitMQ.Client/5.1.2)
* _NB: the version starts at 1.2.0 so as not to collide with previous internal versions._

## License

This work is provided under the terms of the [MIT License](LICENSE).
