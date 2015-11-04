UNICORN README

Thanks for installing Unicorn! Here are some tips to get you started:

First, you should set up a configuration. This tells Unicorn what you want to keep serialized, among other things. 
Make a copy of App_Config/Include/Unicorn/Unicorn.Configs.Default.example and rename it to .config. You can place this file anywhere in App_Config/Include, such as Include/MySite/Unicorn.Configs.MySite.config.
Review the comments in the example configuration file and edit the values as you see fit (especially the predicate settings, which control what is included).

It's probably also worth it to review the other App_Config\Include\Unicorn\*.config files too: make sure they're to your liking.
In particular, the TargetDataStore in Unicorn.config controls where serialized items are written to (it defaults to $(dataFolder)\Unicorn)

Run a build, so that the Unicorn assemblies are copied to your bin folder. If you develop out of webroot you may need to deploy or something as well.
Open the Unicorn control panel (defaults to /unicorn.aspx)
In the control panel, click the Reserialize button to make sure all of the items you want to keep synced are written to disk.

Check your serialized items into source control, and use the Sync in the control panel to move updates from others into your database.
You can also automate doing a sync as part of a CI deployment process; see the readme on GitHub linked below.

Want deeper documentation? The README.md on GitHub is your friend: https://github.com/kamsar/Unicorn/blob/master/README.md, as well as the comments in all the config files.

TFS USERS
- Make sure to make a copy of App_Config\Include\Rainbow.TFS.config.example and rename it to .config so that Unicorn won't use '$' in filenames.
- Looking for TFS 2010/Server Workspace support? Follow the instructions on the TFS Plug-In for Rainbow README.md: https://github.com/PetersonDave/Rainbow.Tfs.

Have questions? Tweet @kamsar.

Found a bug? Send me a pull request on GitHub if you're feeling awesome: https://github.com/kamsar/Unicorn
(or an issue if you're feeling lazy)