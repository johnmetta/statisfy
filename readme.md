# Statsify

Statsify is a set of software components for collecting, aggregating, storing, reporting, graphing and analysis of time series data. It can be used to monitor computer systems and applications alike.

Stataify is built for Microsoft Windows using Microsoft .NET Framework.

## Why Statsify?

> Measurement is the first step that leads to control and eventually to improvement. If you can't measure something, you can't understand it. If you can't understand it, you can't control it. If you can't control it, you can't
improve it.
> ― H. James Harrington

When it comes to collecting metrics on Windows, there's really very little choice: it's etiher Performance Counters or nothing. Or something custom-built, for that matter.

Performance Counters are a disaster from a lot of standpoints. To create them, one has to remember about permissions, call into cumbersome API with lots of alien concepts like Performance Counter Categories, Performance Counter Types, and optionally do `installutil` invocations. While using them, one is again facing the same hostile API, overall brittleness, COM exceptions and the like. Finally, when it comes to actually viewing and analyzing them, `perfmon.msc` has very little to offer: suboptimal UI, no historical storage, no analytics, no graphing, no nothing.

Linux has had Graphite and StatsD since forever, and the simplicity and sheer power of those seemingly simple tools is what led us to create Statsify.

## Overview

Statsify draws inspiration from [Graphite](https://github.com/graphite-project), [StatsD](https://github.com/etsy/statsd/), [Ganglia](http://ganglia.sourceforge.net/) and possibly other projects.

### 10,000 Feet View

Conceptually, there are three sides to Statsify. 

First, there are Agent and Aggregator, two Windows Services that do all the grunt work of collecting, aggregating and storing metrics. 

Second, there is a client library which you can use to feed data to the Aggregator.

Third, there is an HTTP API which you can use to retrieve stored metrics.

### Statsify Agent

This is a Windows Service that runs in the background and collects various server-level metrics (i.e. from Windows Performance Counters) and sends them off to Aggregator or any StatsD-compatible server.

### Statsify Aggregator

This is too a Windows Service that is effectively a pure .NET implementation of a StatsD server. As such, it listens to StatsD-compatible UDP datagrams and then aggregates and stores metrics sent to it. 
Additionally, it exposes an HTTP API which can be used to retrieve stored metrics and to apply interesting transformations to them.

### Statsify Client

This is a StatsD-compatible client for talking to Aggregator or any StatsD-compatible server. It is this assembly that enables your .NET application to send arbitrary metrics to Aggregator.

## Getting Started

At the bare minimum you'll need to get Aggregator up and running.

### Prerequisites

Statsify only requires Microsoft .NET Framework 4.0 or later. No other dependencies, really.

### Running, Installing and Configuring Statsify Aggregator

Statsify Aggregator supports two modes of operation: it can run as a standard console application, or it can be installed as a Windows Service. The former is useful for testing things out, while the latter is the only reliable option for production use.

To get Aggregator up and running, just launch the `statsify.aggregator.exe`. When you do so for the first time, it will create a `statsify-aggregator.config` configuration file in `%PROGRAMDATA%\Statsify\Aggregator`.

To install Aggregator as a Windows Service, open up Command Prompt and run the following command:

    statsify.aggregator install --sudo
    statsify.aggregator start
    
And that's it: now Aggregator is installed and is up and running in the background, waiting for the first UDP packed to arrive.

#### Configuration

If you open up `statsify-aggregator.config`, you'll see quite a few options that configure Aggregator's behavior.

Statsify here borrows a lot  from Graphite, so when something's not clear enough, please refer to the [Configuring Carbon](http://graphite.readthedocs.org/en/latest/config-carbon.html) section of Graphite Documentation.

* `udp-endpoint`: Sets `@address` and `@port` that Aggregator is listening on for UDP packets
* `http-endpoint`: Sets `@address` and `@port` that Aggregator is listening on for incoming HTTP API requests
* `storage`: Configures Aggregator's behavior when it comes to figuring out where and how to store metrics (See [storage-schemas.conf](http://graphite.readthedocs.org/en/latest/config-carbon.html#storage-schemas-conf)):
 * `@path`: An absolute path to where Aggregator will store Datapoint Databases
 * `@flush-interval`: How often will Aggregator save metrics to disk
 * `store`: Configures a single policy of how to store a particular set of metrics (matched by `@pattern`
   * `retention`: Configures a single retention rule. Both `@precision` and `@history` can accept either standard .NET `TimeSpan` string representations (`hh:mm:ss`) or simpler strings like `10s`, `20m`, `1h`, `31d`, `1y`.
* `downsampling`: Configures how Aggregator downsamples metrics (See [storage-aggregation.conf](http://graphite.readthedocs.org/en/latest/config-carbon.html#storage-aggregation-conf)):
 * `downsample`: Configures a single downsampling rule for s set of metrics matched by `@pattern`

### Running, Installing and Configuring Statsify Agent

Agent is very similar in it's nature to Aggregator. It supports exactly the same modes of operations and can be installed just as trivially.

When you first launch `statsify.agent.exe`, it will create a `statsify-agent.config` file in `%PROGRAMDATA%\Statsify\Agent`. It already has a few metrics that Agent starts collecting right away:

* CPU Utilization (Total Time, Privileged Time, User Time)
* Logical Disk Utilization
 * Read and Write Queue Lengths
 * Read and Write Bytes/sec

### Collecting Data from Your Applications

To start collecting data from your .NET application, you'll have to use Statsify Client to talk to Aggregator.

## Usage

It's all very simple: reference `Statsify.Client.dll`, initialize an `IStatsifyClient` and start firing off metrics.

### Configuration

Before using `IStatsifyClient` instance, you need to add the following entries to your `App.config`/`Web.config` file:

    <configuration> 
      <configSections>
        <section name="statsify" type="Statsify.Client.Configuration.StatsifyConfigurationSection, Statsify.Client" />
      </configSections>
      
      <!-- ... -->
      
      <statsify host="127.0.0.1" port="" namespace="" />
    </configuration>
    
* `@host` is the address (preferably an IP address) of the Statsify Aggregator.
* `@port` is the port the Statsify Aggregator is listening on. This is optional and defaults to `8125`.
* `@namespace` is the prefix for all metrics. This is usually set to a lowercased underscore-delimited name of the application, like `production.application_name`

### Initialization

    var configuration = ConfigurationManager.GetSection("statsify") as StatsifyConfigurationSection;
    var statsifyClient = 
        new UdpStatsifyClient(configuration.Host, configuration.Port, configuration.Namespace);

### Usage

The `statsifyClient` object is not guaranteed to be thread-safe, so each thread must get its own instance.

## Glossary

_Datapoint_ is a tuple which consists of a _Timestamp_ and a (possibly undefined) _Value_.

_Series_ is an ordered collection of _Datapoints_ spanning certain time range.

_Archive_ is a _Series_ stored inside a _Database_.

_Database_ is a named and ordered collection of _Archives_.

_Metric_ is a _Series_ with a name.

_Sample_ is a _Datapoint_ with a name and type.

