# Statsify

Computer systems and applications monitoring tool.

## Overview

Statsify draws inspiration from [Graphite](https://github.com/graphite-project), [StatsD](https://github.com/etsy/statsd/)

### Statsify.Agent

Statsify Agent collects server-level metrics (i.e. from Windows Performance Counters) and sends them off to Statsify Aggregator or any StatsD-compatible server.

### Statsify.Aggregator

Statsify Aggregator aggregates and stores metrics sent to it.

### Statsify.Client

StatsD-compatible client for talking to Statsify Aggregator or any StatsD-compatible server.

### Statsify.Core

Statsify Database implementation.

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

## Glossary

_Datapoint_ is a tuple which consists of a _Timestamp_ and a _Value_.

_Series_ is an ordered collection of _Datapoints_

_Metric_ is a _Series_ with a name.

