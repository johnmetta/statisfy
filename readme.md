## Statsify

Computer systems and applications monitoring tool.

### Overview

Statsify draws inspiration from [Graphite](https://github.com/graphite-project), [StatsD](https://github.com/etsy/statsd/)

#### Statsify.Agent

Statsify Agent collects server-level metrics (i.e. from Windows Performance Counters) and sends them off to Statsify Aggregator or any StatsD-compatible server.

#### Statsify.Aggregator

Statsify Aggregator aggregates and stores metrics sent to it.

#### Statsify.Client

StatsD-compatible client for talking to Statsify Aggregator or any StatsD-compatible server.

#### Statsify.Core

Statsify Database implementation.

