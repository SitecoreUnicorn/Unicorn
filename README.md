![la la la la](http://kamsar.net/index.php/2015/09/Unicorn-3-What-s-new/Unicorn_logo.png)

Unicorn is a utility for Sitecore that solves the issue of moving templates, renderings, and other database items between Sitecore instances. This becomes problematic when developers have their own local instances - packages are error-prone and tend to be forgotten on the way to production. Unicorn solves this issue by writing serialized copies of Sitecore items to disk along with the code - this way, a copy of the necessary database items for a given codebase accompanies it in source control.

For basic usage, Unicorn has two moving parts:
* Data provider - The default Sitecore data provider is extended to automatically serialize item changes as they are made to Sitecore. This means that at any given time, what's serialized is the "master copy."
* Control panel - this tool (/unicorn.aspx) is a page that can sync the state of Sitecore to the state stored on disk (respecting presets and exclusions). You do this after you pull down someone else's serialized changes from source control.

Unicorn avoids the need to manually select changes to merge unlike some other serialization-based solutions because the disk is always kept up to date by the data provider. This means that if you pull changes in from someone else's Sitecore instance you will have to immediately merge and/or conflict resolve the serialized files in your source control system - leaving the disk always the master copy of everything. Then if you execute the sync page, the merged changes are synced into your Sitecore database. Unicorn uses the [Rainbow](https://github.com/kamsar/Rainbow) serialization engine, which uses some format enhancements that make it far simpler to merge than the Sitecore default format.

## Is this like TDS?

Unicorn solves some of the same issues as [Hedgehog's TDS](https://www.hhogdev.com/products/team-development-for-sitecore/overview.aspx). The major difference in approach is that because Unicorn forces all of the merging to be done on the disk, you never have to manually select what to update when you're running a sync operation or remember to write changed items to disk. Unless you have actual collisions, this saves a lot of time because you can take advantage of Git, SVN, TFS, etc to do automerges for you. That said, TDS and Unicorn have different feature sets and goals. TDS is a monolithic product with commercial support and marketing that does a lot more than just serialization. Unicorn is relatively simple, free and open source, and does one thing well. Use whatever makes you happy :)

## Initial Setup
* Upgrading from Unicorn 3? Great: it's just a NuGet upgrade away. From [Unicorn 2.x](https://github.com/kamsar/Unicorn/wiki/Upgrading-to-Unicorn-3), you can follow the same directions for 2.x->3.x to go from 2 to 4.
* You'll need Sitecore 7 or later (including 8.x).
* Install Unicorn. This is as simple as adding the Unicorn NuGet package to your project.
* When you install the NuGet package, a [README file](https://github.com/kamsar/Unicorn/blob/master/Build/Unicorn.nuget/readme.txt) will come up in Visual Studio with help to get you started.

## Using Unicorn
When using Unicorn it's important to follow the expected workflow.

* When you update/pull from your source control system, execute the Sync operation using `/unicorn.aspx` if any changes to .yml files were present. Note: Unicorn supports _transparent syncing_ which allows you to skip this step during development.
* When you commit to source control, include your changed items along with your code changes. Unicorn will automatically serialize item changes you make in Sitecore to items that match the predicate(s) configured.
* Conflicts in items are resolved at the source control level - at any given time, the disk is considered the master copy of the Sitecore items (due to local changes being automatically serialized as they're made)

## Unicorn Features

There are a few special features that Unicorn has that are worth mentioning.

* You can define multiple _configurations_, which allow you a lot of flexibility: you can serialize items to different places on disk, set up groups that can be synced separately, and override any aspect of Unicorn in each configuration. Configurations may also define dependencies between each other to enforce a hierarchy.
* Using Rainbow gives Unicorn a unique, easy to manage [serialization format](http://kamsar.net/index.php/2015/07/Rethinking-the-Sitecore-Serialization-Format-Unicorn-3-Preview-part-1/) and hierarchy.
* Unicorn rejects "inconsequential" changes to items. The Sitecore Template Editor likes to make a lot of item saves that change nothing but the last modified date and revision. These are ignored to reduce churn in your source control.
* During a sync operation, Unicorn can detect improperly merged renamed items (e.g. two serialized items with the same ID in different files) and will report that fact as an error.
* Automatic retries are performed in the event of a load failure during a sync, which means that syncing items with a missing template along with the template itself in the same sync session will work correctly.
* Unicorn's logging routines report on exactly what was changed about a deserialized item (changed fields, added/removed versions, moved, renamed, etc)
* The control panel writes all console output - e.g. of a sync - to both the screen and the Sitecore log file. This provides a handy audit trail of what synchronizations did in the event of someone asking where an item went.
* You can use `FieldFilter`s to ignore deserialization and changes to specific fields you don't want to sync.
* The automatic serialization cannot be blocked by event disabling code because it runs at a data provider level.
* Content editor warnings are shown for items that Unicorn is controlling.
* You can define custom ways to compare fields, if there are equivalencies that are more complex than string equality
* There are event pipelines that can be used to hook to sync events. These can be used, for example, to auto-publish synced items.
* Transparent syncing allows the data provider to read directly from the serialized items on disk, making them appear directly in Sitecore.
* Super-fast syncing with the 'Dilithium' direct-SQL item reading engine (optional, considered mostly stable but sitll experimental)
* Integration with [Sitecore PowerShell Extensions](https://www.gitbook.com/book/sitecorepowershell/sitecore-powershell-extensions/details) to perform Unicorn tasks

You can find further details about Unicorn 4 at Kam's blog:
* [Dilithium and Performance](https://kamsar.net/index.php/2017/02/Unicorn-4-Preview-Project-Dilithium/)
* [Sitecore PowerShell + Unicorn](https://kamsar.net/index.php/2017/02/Unicorn-4-Preview-Part-2-SPE-Support/)
* [Packaging Unicorn Configurations with SPE](https://kamsar.net/index.php/2017/02/Unicorn-4-Preview-Part-2-5-Generating-Packages-with-SPE/)
* [Configuration Inheritance and Convention Support](https://kamsar.net/index.php/2017/02/Unicorn-4-Part-III-Configuration-Enhancements/)

There is also a series of blog posts detailing Unicorn 3 (and why the Rainbow serialization format exists) at Kam's blog, which are still relevant:
* [Unicorn 3: What's New?](http://kamsar.net/index.php/2015/09/Unicorn-3-What-s-new/)
* [What is Rainbow?](http://kamsar.net/index.php/2015/09/Rainbow-Part-3-What-is-Rainbow/)
* [Rethinking the Sitecore Serialization Format with Rainbow](http://kamsar.net/index.php/2015/07/Rethinking-the-Sitecore-Serialization-Format-Unicorn-3-Preview-part-1/)
* [Reinventing the Serialization File System with Rainbow](http://kamsar.net/index.php/2015/08/Reinventing-the-Serialization-File-System-Rainbow-Preview-Part-2/)

## Best Practices

* If using [Helix architecture](http://helix.sitecore.net/) plan to use one Unicorn configuration per module. You can use [Unicorn 4's configuration conventions](https://kamsar.net/index.php/2017/02/Unicorn-4-Part-III-Configuration-Enhancements/) to avoid repetition and promote a consistent item architecture across modules.
* Make sure to enable and disable configuration files appropriately when developing, and deploying to CE/CM and CD environments. Each config file has a comment header that details when it should be enabled and disabled. Most important is to disable the data provider config when deployed.
* Always automate your deployments of Unicorn items (see _Automated Deployment_ below). This ensures a consistent item state during deployments.
* Consider automating _local development_ Unicorn syncing using scripts (e.g. PowerShell or Gulp), which can enable you to have a one-click "post-pull" experience that both builds your projects and syncs any item changes in. [Sitecore Habitat](https://github.com/sitecore/habitat) does this using a combination of Gulp and the Unicorn PowerShell API.

## Automated Deployment

With Unicorn you've got two options for automated deployment of item changes, for example from a Continuous Integration server or production deploy scripts.

### Use Transparent Sync
When using [Transparent Sync](http://kamsar.net/index.php/2015/10/Unicorn-Introducing-Transparent-Sync/), the items on disk magically appear in Sitecore without syncing. So one automated deployment option is to simply use Transparent Sync and copy your updated serialized items to the deployment target alongside your code. This is advantageous because it's simple to set up and requires no direct intervention with the deployed server after deployment (e.g. a HTTP call).

### Use the Automated Tool API
Unicorn has an automated tool API whereby you can invoke actions in the Unicorn control panel from a script, such as invoking a sync after a code deployment.

**NOTE: Automated Tool API is completely overhauled in Unicorn 3.1 and later, and these instructions are for 3.1+ only**

Tools are authenticated using a shared secret between the tool and the Sitecore server running Unicorn, which is relayed via [CHAP](https://en.wikipedia.org/wiki/Challenge-Handshake_Authentication_Protocol)+[HMAC](https://en.wikipedia.org/wiki/Hash-based_message_authentication_code)-SHA512. The practical upshot of this is that the shared secret never travels over the wire, the authentication key is unique every time, and replay attacks are not possible. You should still use the tool API over a TLS (SSL) connection if possible.

Calls to the control panel from an automated tool behave a little differently from interactive control panel sessions. Specifically:

* Automated calls are not streaming (nothing is written to the response until everything is complete)
* Automated calls return HTTP 500 if an error occurs (interactive calls that fail return HTTP 200, because the HTTP headers have been sent long before the error occurs). In the example PS script above, this will throw a PowerShell exception.
* The output is text formatted, instead of HTML formatted, so it is much easier to read in logs.

Ok, ok. Shut up about crypto and tell me how to set it up.

1. Generate a very long random shared secret key, preferably using a password generator. There are no limits on character count, character types, etc but it must be > 30 characters.
2. Install the shared secret into the `Unicorn.UI.config` file - or a patch thereof, under the `authenticationProvider/SharedSecret` node. There are comments to help.
3. To call the tool API from a script, a PowerShell module is provided. Acquire the module and its supporting files from the `packages\Unicorn.version\tools\PSAPI` folder of where you installed the Unicorn NuGet package. (Alternatively, it's also in  `doc\PowerShell Remote Scripting` folder of the Unicorn git repository, however this is always the latest and not necessarily matching your installed Unicorn version)
4. Review the `sample.ps1` file and adapt it to your needs, including putting the shared secret into it and setting the URL as needed. Don't worry the guts of `sample.ps1` are two simple lines of code :)

NOTE: When deploying to a Content Editing or Content Delivery server, the Unicorn configuration should be trimmed down from development. Each config file in `App_Config/Include/Unicorn` has comments at the top designating what environment(s) it should live on. If you opt to use Transparent Sync as a deployment mechanism, make sure you do not disable the data provider config file.

[Darren Guy](https://twitter.com/DarrenGuy) has written a practical dissertation on his experiences setting up [Unicorn 3 with TeamCity and Octopus Deploy](https://thesoftwaredudeblog.wordpress.com/2016/05/24/building-a-continuous-integration-environment-for-sitecore-part-8-using-unicorn-for-sitecore-item-syncronization/) that goes all the way from install to automated deployment. A recommended read if you're wanting to use a similar setup.

[Andrew Lansdowne](https://twitter.com/Rangler2) has also written a post _(for version 1, so some of it is outdated but the concepts still apply)_ about [setting up Unicorn with TeamCity and WebDeploy](http://andrew.lansdowne.me/2013/06/07/auto-deploy-sitecore-items-using-unicorn-and-teamcity/) that may be useful when setting up automated deployments.

## Unicorn's Sync Rules

_Note: these rules concern the default Evaluator only. This is probably what makes sense for most people, but be aware you can plug in and change all of this: for example the `NewItemsOnlyEvaluator` only writes new items into Sitecore and ignores all others (see `Unicorn.Configs.NewItemsOnly.example` in the config folder for an example)_

* The disk is considered the master at all times. Because the Unicorn data provider is automatically serializing item changes as they are made in Sitecore, changes you make are already serialized. Others' serialized updates from source control merge just like code.
* "Changed" items are determined by any difference in field values (in shared or versioned fields, across all versions).
* Items that exist in Sitecore but not on disk are deleted, because the disk is the master.

## Pitfalls

* Don't use Unicorn if you have a shared Sitecore database unless only one person is writing changes to it. If person A makes changes, then person B syncs to the shared database, person A's changes will be lost because B's disk is the master. **Do not use a shared Sitecore database!**
* Don't use Unicorn to serialize actively versioned or workflow-enabled content (e.g. non-developer items). You can easily have two people create totally different "version 2" (or even v3, overwriting someone else's v2) content on different locations, and merging those is probably not what you want. It may be relatively safe during initial development if sharing test content, but be wary.

## Manual Installation/Install from Source

* Clone the repository
* Place a copy of your Sitecore.Kernel.dll assembly in /lib/sitecore/v7 (for v7/v8)
* Build the project for your Sitecore version using Visual Studio 2012 or later
* Copy Unicorn.dll, Rainbow.dll, Rainbow.Storage.Sc.dll, Rainbow.Storage.Yaml.dll and Kamsar.WebConsole.dll to your main project in whatever fashion you wish (project reference, as binary references, etc)
* Copy `Standard Config Files\*.config` to the `App_Config\Include\Unicorn` folder
* Configure to your liking; the [setup README file](https://github.com/kamsar/Unicorn/blob/master/Build/Unicorn.nuget/readme.txt) is a good starting point.
* Hit $yoursite/unicorn.aspx to perform initial serialization of your configured predicate

## Advanced Usage and Customization

Unicorn uses a very flexible configuration system based on Dependency Injection that allows you to plug in your own rules for almost any part of what Unicorn does.

### Configurations

The `IConfiguration` is the heart of all Unicorn customizations. This is an abstracted IoC container that contains registrations for all other pluggable types. The container is Unicorn's own very tiny purpose built IoC container 'Micro', and it does not depend on any other DI libraries.

But wait, there's more. You can configure more than one IConfiguration using the IConfigurationProvider. The default provider is registered in `Unicorn.config` (configurationProvider element). It reads configuration from...the `Unicorn.config`. The `defaults` element defines the standard dependency configuration, and the `configurations/*` elements define custom configurations that can override the defaults. Each dependency type can have non-DI constructor params (string or bool) passed to it by adding XML attributes to the main declaration - e.g. `<foo type="..." bar="hello">` would pass "hello" to `public MyType(string bar)`. You can also receive any XML body passed to the dependency to a `configNode` `XmlNode` parameter. This is how the `SerializationPresetPredicate` defines its preset.

### Evaluator

The evaluator is a very powerful thing to customize. Evaluators are responsible for:

* Detecting if a Sitecore item and a serialized item have a change that is a "difference"
* Deciding what to do if a difference is there (overwrite Sitecore? overwrite serialized?)
* Deciding what to do with orphan items (items that are not serialized, but exist in Sitecore - the default would delete them)

For examples check out `Unicorn.Evaluators.SerializedAsMasterEvaluator`, which uses the Rainbow `ItemComparer` to compare items.

### Predicate

The predicate is another powerful customization. Predicates define what items get included and excluded from Unicorn - for both automatic serialization and the sync process. 

The default predicate uses _serialization presets_, but it's easy to imagine other possibilities such as a rules engine based preset.

For examples see `Unicorn.Predicates.SerializationPresetPredicate`

### Field Filter

The Field Filter is a way to exclude certain fields from being controlled by Unicorn. Note that the control is not complete in that the value of ignored fields is never stored; it is stored and updated when other fields' values that are included change. However it is never deserialized or considered in the evaluator, and thus the value is effectively ignored.

For examples see `Rainbow.Filtering.ConfigurationFieldFilter`

### Target Data Store

The target data store defines where we are writing serialized items to. The default target data store uses Rainbow's SFS tree structure and YAML serialization formatter.

For examples see the Rainbow project's various `IDataStore` implementations.

### Source Data Store

The source data store is another Rainbow `IDataStore` that defines where we read values from and sync values to. Normally this is the Rainbow Sitecore data store, however you could also hook up a sync say between two separate serialization formats, or a serialization database - your imagination is the limit :)

### Loader

The loader class encapsulates the logic of walking the tree in the Serialization Provider and comparing it to the tree in the Source Data Provider. It checks the Predicate to determine inclusion, and invokes the Evaluator to determine how to deal with changes.

Normally this will not require customization, as its dependencies provide the extension points.

Example: `Unicorn.Loader.SerializationLoader`

### Data Provider Architecture

There are two components to the Unicorn data provider: the database-specific implementation, and the Unicorn implementation.

The Unicorn implementation is an individual configuration of Unicorn dependencies that get automatic serialization. For example, if you were serializing two presets you'd need two instances of `UnicornDataProvider` - one for each `IPredicate` implementation.

The database specific implementation is a subclass of the original Sitecore data provider that provides a container for one or more `UnicornDataProvider` instances. Out of the box, a `UnicornSqlServerDataProvider` is provided. You could roll your own if you're on Oracle. This provider is effectively an unblockable event handler that allows Unicorn to trap item changes even if the evil `EventDisabler` class is being used.

If you want to wire multiple Unicorn data providers to your database, you create a class that derives from `UnicornSqlServerDataProvider`. In this class you can select to:

* Create a constructor that injects your provider(s) using the base constructor: 

        public MyDataProvider(string connectionString) : base(connectionString, new UnicornDataProvider(), new UnicornDataProvider(), ...)
	
* Create a constructor that injects your provider(s) using code (this is better if you have to construct dependencies, etc that don't fit well in a base call):


		 public MyDataProvider(string connectionString) : base(connectionString, null)
		 {
			AddUnicornDataProvider(new UnicornDataProvider());
			// ...
		 }

## TL;DR

Well you just read that didn't you? If you have questions or bugs, feel free to open an issue.

You can also find help on Sitecore Community Slack in [#unicorn](https://sitecorechat.slack.com/messages/unicorn/) or on Twitter ([@kamsar](https://twitter.com/kamsar)).
