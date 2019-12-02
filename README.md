# AddUp.RabbitMQ.Fakes

[![codecov.io](https://codecov.io/github/AddUpSolutions/AddUp.RabbitMQ.Fakes/coverage.svg?branch=master)](https://codecov.io/github/AddUpSolutions/AddUp.RabbitMQ.Fakes?branch=master)
[![Build Status](https://dev.azure.com/addupsolutions/AddUp.RabbitMQ.Fakes/_apis/build/status/addupsolutions.AddUp.RabbitMQ.Fakes?branchName=master)](https://dev.azure.com/addupsolutions/AddUp.RabbitMQ.Fakes/_build/latest?definitionId=3&branchName=master)

## About

**AddUp.RabbitMQ.Fakes** is a fork of <https://github.com/Parametric/RabbitMQ.Fakes>.

**AddUp.RabbitMQ.Fakes** is a library that contains fake implementations of the **RabbitMQ.Client** interfaces. These are intended to be used for testing so that unit tests that depend on RabbitMQ can be executed fully in memory withouth needing an external RabbitMQ server.

**AddUp.RabbitMQ.Fakes** builds on top of the original project:

* Targets .NET Standard 2.0
* Supports Default, Direct, Topic and Fanout exchange types.
  * NB: Headers exchange type is not supported; however, it won't throw (it is implemented the same way as the Fanout type)

## License

This work is provided under the terms of the [MIT License](LICENSE).
