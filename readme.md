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

