# Unicorn

Unicorn is a utility for Sitecore that solves the issue of moving templates, renderings, and other database items between Sitecore instances. This becomes problematic when developers have their own local instances - packages are error-prone and tend to be forgotten on the way to production. Unicorn solves this issue by using the Serialization APIs to keep copies of Sitecore items on disk along with the code - this way, a copy of the necessary database items for a given codebase accompanies it in source control.

For basic usage, Unicorn 2 has two moving parts:
* Data provider - The default Sitecore data provided is extended to automatically serialize item changes as they are made to Sitecore. This means that at any given time, what's serialized is the "master copy."
* Control panel - this tool (/unicorn.axd) is a page that can sync the state of Sitecore to the state stored on disk (respecting presets and exclusions). You do this after you pull down someone else's serialized changes from source control.

Unicorn avoids the need to manually select changes to merge unlike some other serialization-based solutions because the disk is always kept up to date by the data provider. This means that if you pull changes in from someone else's Sitecore instance you will have to immediately merge and/or conflict resolve the serialized files in your source control system - leaving the disk still the master copy of everything. Then if you execute the sync page, the merged changes are synced into your Sitecore database.

Before using Unicorn you should review the Sitecore Serialization Guide available on SDN (http://sdn.sitecore.net/upload/sitecore7/70/serialization_guide_sc70-a4.pdf) and familiarize yourself with how item serialization works. Make sure to read the 'Pitfalls' section below as well :)

## Initial Setup
* Install Unicorn. This is as simple as adding the Unicorn NuGet package to your project.
* Configure what to serialize as a serialization preset. There will be an `App_Config/Include/Serialization.config` file installed, which has a commented example of this syntax.
* Visit $yoursite/unicorn.axd and tell it to run the initial serialization. This will take the preset you configured and serialize all of the included items in it to disk. *NOTE: make sure you serialize an authoritative database with all items present. Other databases will be made to look just like this one when sync occurs.*
* Commit your serialized items to source control

## Using Unicorn
When using Unicorn it's important to follow the expected workflow.

* When you update/pull from your source control system, you should execute the Sync operation on /unicorn.axd if any changes to .item files were present
* When you commit to source control, include your changed items along with your code changes
* Conflicts in items are resolved at the source control level - at any given time, the disk is considered the master copy of the Sitecore items (due to local changes being automatically serialized as they're made)

## Unicorn Features

There are a few special features that Unicorn has that are worth mentioning.

* Unicorn rejects "inconsequential" changes to items. Specifically the Sitecore Template Editor likes to make a lot of item saves that change nothing but the last modified date and revision. These are ignored to reduce churn in your source control.
* During a sync operation, Unicorn can detect improperly merged renamed items (e.g. two serialized items with the same ID in different files) and will report that fact as an error.
* Automatic retries are performed in the event of a load failure during a sync, which means that syncing items with a missing template along with the template itself in the same sync session will work correctly.
* A custom deserialize routine allows Unicorn to report on exactly what was changed about a deserialized item (changed fields, added/removed versions, etc)
* The control panel writes all console output - e.g. of a sync - to both the screen and the Sitecore log file. This provides a handy audit trail of what synchronizations did in the event of someone asking where an item went.

## Automated Deployment

Using Unicorn for automated deployment is easy - simply configure your CI server to make a HTTP call to the control panel after deploying the site. 

The Unicorn control panel looks for a pre-shared "Authenticate" HTTP header that is used when doing automated deployments. Simply define the key as a "DeploymentToolAuthToken" appSetting:

    <add key="DeploymentToolAuthToken" value="generate-a-long-random-string-for-this" />
	
Then when your build script calls the Unicorn control panel simply pass this key value along as an Authenticate HTTP header. For example using PowerShell 3.0 or later you would use this:

	$url = 'http://your-site-authoring-url/unicorn.axd?verb=Sync'
	$deploymentToolAuthToken = 'generate-a-long-random-string-for-this'
    $result = Invoke-WebRequest -Uri $url -Headers @{ "Authenticate" = $deploymentToolAuthToken } -TimeoutSec 10800 -UseBasicParsing
	
	Write-Host $result.Content
	
Calls to the control panel from an automated script behave a little differently from interactive calls do. Specifically:

* Automated calls are not streaming (e.g. nothing is written to the response until everything is complete)
* Automated calls return HTTP 500 if an error occurs (interactive calls that fail return HTTP 200, because the HTTP headers have been sent long before the error occurs). In the example PS script above, this will throw a PowerShell exception.
* The output is text formatted, instead of HTML formatted, so it is much easier to read in logs.

NOTE: When deploying to a Content Delivery server, the Unicorn Control Panel HTTP Handler and the Serialization.config file should be removed for security and performance reasons. When deploying to a Content Authoring server, you can remove the data provider configuration unless you need to track content changes made there for some reason.

[Andrew Lansdowne](https://twitter.com/Rangler2) has also written a post _for version 1, so some of it is outdated_ about [setting up Unicorn with TeamCity and WebDeploy](http://andrew.lansdowne.me/2013/06/07/auto-deploy-sitecore-items-using-unicorn-and-teamcity/) that may be useful when setting up automated deployments.

## Unicorn's Sync Rules

_Note: these rules concern the default Evaluator only. This is probably what makes sense for most people, but be aware you can plug in and change all of this!_

* The disk is considered the master at all times. Because the Unicorn data provider is automatically serializing item changes as they are made in Sitecore, changes you make are already serialized. Others' serialized updates from source control merge just like code.
* "Changed" items are determined by any difference in modified date (not only newer times on disk - ANY difference, because disk is the master copy) or the revision field.
* Items that exist in Sitecore but not on disk are deleted, because the disk is the master.

## Pitfalls

* Don't use Unicorn if you have a shared Sitecore database unless only one person is writing changes to it. If person A makes changes, then person B syncs to the shared database, person A's changes will be lost because B's disk is the master. Do not use a shared Sitecore database!
* Don't use Unicorn to serialize versioned or workflow-enabled content (e.g. non-developer items). You can easily have two people create totally different "version 2" (or even v3, overwriting someone else's v2) content on different locations, and merging those is probably not what you want. It may be relatively safe during initial development if sharing test content, but be wary.
* If you delete a template field, make sure to re-serialize any items you have under Unicorn that use that template - for example a standard values item. Extra fields will cause a loading error (which is good because otherwise you might lose data that would never be detected as a change).

## Manual Installation/Install from Source

* Clone the repository
* Place a copy of your Sitecore.Kernel.dll assembly (Sitecore 7+) in /lib/sitecore
* Build the project using Visual Studio 2012 or later
* Copy Unicorn.dll and Kamsar.WebConsole.dll to your main project in whatever fashion you wish (project reference, as binaries, etc)
* Copy Serialization.config to your App_Config\Include folder
* Register the Unicorn Control Panel under `system.webServer/handlers` in your Web.config: `<add name="Unicorn" path="unicorn.axd" verb="GET" type="Unicorn.ControlPanel.ControlPanelHandler, Unicorn" />`
* Configure Serialization.config to your liking
* Hit $yoursite/unicorn.axd to perform an initial serialization of your configured predicate

## Advanced Usage and Customization

Unicorn 2 uses a very flexible configuration system based on Dependency Injection that allows you to plug in your own rules for almost any part of what Unicorn does.

### Dependency Registry

The `IDependencyRegistry` is the heart of all Unicorn customizations. This is an abstracted IoC container that contains registrations for all other pluggable types. The default implementation uses a built-in copy of the [TinyIoC](https://github.com/grumpydev/TinyIoC) library, but it is generic enough to get wired to any IoC container if you prefer your own.

The default registry is registered in Serialization.config, so you can wholesale change the default wirings by changing the class that is configured there to your own implementation. The real power comes however in being able to make copies of the default config (using the `Registry` class) and inject your own 'temporary dependencies' into the copy. This enables scenarios like serializing multiple presets to different locations on the filesystem and other advanced usages.

For examples of usage, check out the `Unicorn.ControlPanel` namespace, specifically `SyncConsole`, as well as the `Unicorn.Dependencies.DefaultDependencyRegistry` class.

### Evaluator

The evaluator is a very powerful thing to customize. Evaluators are responsible for:

* Detecting if a Sitecore item and a serialized item have a change that is a "difference"
* Deciding what to do if a difference is there (overwrite Sitecore? overwrite serialized?)
* Deciding what to do with orphan items (items that are not serialized, but exist in Sitecore - the default would delete them)

For examples check out `Unicorn.Evaluators.SerializedAsMasterEvaluator`

### Predicate

The predicate is another powerful customization. Predicates define what items get included and excluded from Unicorn - for both automatic serialization and the sync process. 

The default predicate uses _serialization presets_, but it's easy to imagine other possibilities such as a rules engine based preset.

For examples see `Unicorn.Predicates.SerializationPresetPredicate`

### Serialization Provider

The serialization provider defines the way that items are serialized. The default serialization provider writes Sitecore standard .item files to disk.

You could just as much implement a SQL-based serialization provider, serialize as JSON, or other ideas if you wanted to.

For examples see `Unicorn.Serialization.Sitecore.SitecoreSerializationProvider` and `Unicorn.Serialization.Sitecore.Fiat.FiatSitecoreSerializationProvider` (the Fiat provider uses a custom deserializer that gives better diagnostics)

### Source Data Provider

The `ISourceDataProvider` is responsible for retrieving data and performing updates to the source data system - Sitecore. While this could hypothetically connect to something else, it has enough Sitecore-specific requirements in the data model that make it unlikely you'd want to do that.

### Loader

The loader class encapsulates the logic of walking the tree in the Serialization Provider and comparing it to the tree in the Source Data Provider. It checks the Predicate to determine inclusion, and invokes the Evaluator to determine how to deal with changes.

Normally this will not require customization, as its dependencies provide the extension points.

Example: `Unicorn.Loader.SerializationLoader`

### Data Provider Architecture

There are two components to the Unicorn data provider: the database-specific implementation, and the Unicorn implementation.

The Unicorn implementation is an individual configuration of Unicorn dependencies that get automatic serialization. For example, if you were serializing two presets you'd need two instances of `UnicornDataProvider` - one for each `IPredicate` implementation.

The database specific implementation is a subclass of the original Sitecore data provider that provides a container for one or more `UnicornDataProvider` instances. Out of the box, a `UnicornSqlServerDataProvider` is provided. You could roll your own if you're on Oracle. This provider is effectively an unblockable event handler that allows Unicorn to trap item changes even if the evil `EventDisabler` class is being used.

If you want to wire multiple Unicorn data providers to your database, you create a class that derives from `UnicornSqlServerDataProvider`. In this class you can select to:

* Create a constructor that injects your provider(s) using the base constructor: `public MyDataProvider(string connectionString) : base(connectionString, new UnicornDataProvider(), new UnicornDataProvider(), ...)`
* Create a constructor that injects your provider(s) using code (this is better if you have to construct dependencies, etc that don't fit well in a base call):

     public MyDataProvider(string connectionString) : base(connectionString, null)
	 {
		AddUnicornDataProvider(new UnicornDataProvider());
		// ...
	 }

## TL;DR

Well you just read that didn't you? If you have questions or bugs, feel free to open an issue.