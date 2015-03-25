# Statsify

Statsify is collection of software for collecting, aggregating, storing, reporting, graphing and analysis of time series data. It can be used to monitor computer systems and applications alike.

Statsify is built primarily for Microsoft Windows. 

## Why Statsify?

Lots of reasons. First of all, Windows platform has nothing like Graphite or StatsD

## Overview

Statsify draws inspiration from [Graphite](https://github.com/graphite-project), [StatsD](https://github.com/etsy/statsd/), [Ganglia](http://ganglia.sourceforge.net/) and possibly other projects. If you

### 10,000 Feet View

Conceptually, there are three sides to Statsify. 

First, there are Agent and Aggregator, two Windows Services that do all the grunt work of collecting, aggregating and storing metrics. 

Second, there is a client library which you can use to feed data to the Aggregator.

Third, there is an HTTP API which you can use to retrieve stored metrics.

### Statsify Agent

This is a Windows Service that runs in the background and collects various server-level metrics (i.e. from Windows Performance Counters) and sends them off to Statsify Aggregator or any StatsD-compatible server.

### Statsify Aggregator

This is too a Windows Service that is effectively a pure .NET implementation of a StatsD server. As such, it listens to StatsD-compatible UDP datagrams and then aggregates and stores metrics sent to it. 

Additionally, it exposes an HTTP API which can be used to retrieve stored metrics and to apply interesting transformations to them.

### Statsify Client

This is a StatsD-compatible client for talking to Statsify Aggregator or any StatsD-compatible server. It is this assembly that enables your .NET application to send arbitrary metrics to Statsify Aggregator.

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
    
And that's it: now Statsify Aggregator is installed and is up and running in the background, waiting for the first UDP packed to arrive.

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

Statsify Agent is very similar in it's nature to Statsify Aggregator. It supports exactly the same modes of operations, it too creates  and can be installed just as easy.

When you first launch `statsify.agent.exe`, it will create a `statsify-agent.config` file in `%PROGRAMDATA%\Statsify\Agent`. It already has a few metrics that Agent starts collecting right away:

* CPU Utilization (Total Time, Privileged Time, User Time)
* Logical Disk Utilization
 * Read and Write Queue Lengths
 * Read and Write Bytes/sec

## Usage

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
    IStatsifyClient statsifyClient = 
        new UdpStatsifyClient(
            configuration.Host, 
            configuration.Port, 
            configuration.Namespace);

### Usage

The `statsifyClient` object is not guaranteed to be thread-safe, so each thread must get its own instance.

## Practical Guide

> This is taken from http://matt.aimonetti.net/posts/2013/06/26/practical-guide-to-graphite-monitoring/

### Namespacing

Always namespace your collected data, even if you only have one app for now. If your app does two things at the same time like serving HTML and providing an API, you might want to create two clients which you would namespace differently.

#### Naming metrics

Properly naming your metrics is critical to avoid conflicts, confusing data and potentially wrong interpretation later on. 
I like to organize metrics using the following schema:
    
    <namespace>.<instrumented section>.<target (noun)>.<action (past tense verb)>

Example:

    accounts.authentication.password.attempted
    accounts.authentication.password.succeeded
    accounts.authentication.password.failed

I use nouns to define the target and past tense verbs to define the action. This becomes a useful convention when you need to nest metrics. In the above example, let’s say I want to monitor the reasons for the failed password authentications. Here is how I would organize the extra stats:

    accounts.authentication.password.failure.no_email_found
    accounts.authentication.password.failure.password_check_failed
    accounts.authentication.password.failure.password_reset_required

As you can see, I used `failure` instead of `failed` in the stat name. The main reason is to avoid conflicting data. `failed` is an action and already has a data series allocated, if I were to add nested data using `failed`, the data would be collected but the result would be confusing. The other reason is because when we will graph the data, we will often want to use a wildcard * to collect all nested data in a series.

Graphite wild card usage example on counters:

    accounts.authentication.password.failure.*

This should give us the same value as `accounts.authentication.password.failed`, so really, we should just collect the more detailed version and get rid of accounts.authentication.password.failed.

Following this naming convention should really help your data stay clean and easy to manage.

### Counters and metrics

Use counters for metrics when you don’t care about how long the code your are instrumenting takes to run. Usually counters are used for data that have more of a direct business value. Examples include sales, authentication, signups, etc.

Timers are more powerful because they can be used to analyze the time spent in a piece of code but also be used as a counters. Most of my work involves timers because I want to detect system anomalies including performance changes and trends in the way code is being used.

### Glossary

_Datapoint_ is a tuple which consists of a _Timestamp_ and a (possibly undefined) _Value_.

_Series_ is an ordered collection of _Datapoints_ spanning certain time range.

_Archive_ is a _Series_ stored inside a _Database_.

_Database_ is a named and ordered collection of _Archives_.

_Metric_ is a _Series_ with a name.

_Sample_ is a _Datapoint_ with a name and type.

