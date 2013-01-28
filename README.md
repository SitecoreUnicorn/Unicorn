# Unicorn

Unicorn is a utility for Sitecore that solves the issue of moving templates, renderings, and other database items between instances. It accomplishes this with some tweaks to the way the default item serialization works and some conventions.

There are two major pieces to Unicorn:
* Event Handlers - these are based on the default Item Serialization handlers, but they interface with the serialization preset system. Once these are attached, any changes within the Sitecore site that match the serialization preset are automatically updated to disk.
* Serialization Sync Tool - this tool is a page that is run that syncs the state of Sitecore to the state stored on disk (respecting presets and exclusions).

## Initial Setup
* Install Unicorn. This can be as simple as referencing Unicorn.dll, copying the Serialization.config to App_Config/Include, and installing the Serialization Sync Tool page somewhere convenient.
* Configure what to serialize as a serialization preset. Usually you'd use the Documentation/Serialization.config config-include file to do this (there's an example there already)
* Next use the serialization page (/sitecore/admin/serialization.aspx) to serialize the preset to disk so you have a baseline copy of the items
* Commit your serialized items to source control

## Using Unicorn
When using Unicorn it's important to follow the expected workflow.

* When you update/pull from your source control system, you should execute the Sync Tool page if any changes to .item files were present
* When you commit to source control, include your changed items along with your code changes
* Conflicts in items are resolved at the source control level - at any given time, the disk is considered the master copy of the Sitecore items (due to local changes being automatically serialized as they're made)

## Automated Deployment

Using Unicorn for automated deployment is easy - simply configure your CI server to make a HTTP call to the sync page after deploying the site. The default sync tool page requires you to either be an administrator, or pass an appropriate user-configured token in the Authenticate HTTP header to run the page.

When deploying to a Content Delivery server, the Unicorn sync tool page and serialization configuration should be removed.

## Unicorn's Sync Rules

* The disk is considered the master at all times. The event handlers make this usable, because local changes are already serialized on disk (and so updates from SCM must merge with local at that time just like code)
* "Changed" items are determined by any difference in modified date (not only newer times on disk - ANY difference, because disk is the master copy)
* Items that exist in Sitecore but not on disk are deleted, because the disk is the master.

## Pitfalls

* Don't use Unicorn if you have a shared Sitecore database unless only one person is writing changes to it. If person A makes changes, then person B syncs to the shared database, person A's changes will be lost because B's disk is the master. Do not use a shared Sitecore database!
* Don't use Unicorn to serialize versioned or workflow-enabled content (e.g. non-developer items). You can easily have two people create totally different "version 2" (or even v3, overwriting someone else's v2) content on different locations, and merging those is probably not what you want. It may be relatively safe during initial development if sharing test content, but be wary.

## Suggestions

* If you have a large installation, consider splitting it into multiple presets for speed (for example, a "core" preset, "templates" preset, etc). This way you could sync only the preset that had updates. The "default" preset should probably still sync everything.