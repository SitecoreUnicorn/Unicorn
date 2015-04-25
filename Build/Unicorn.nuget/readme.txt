UNICORN README

Thanks for installing Unicorn! Here are some tips to get you started:

First, you should set up your configuration. 
Open App_Config/Include/Unicorn/Unicorn.config and configure the predicate to include the paths in Sitecore you wish to keep serialized.
It's probably also worth it to review the other Unicorn.*.config files there too, to make sure they're to your liking.

Next you'll want to run a build, and open the Unicorn control panel (defaults to /unicorn.aspx)
In the control panel, click the Reserialize button to make sure all of the items you want to keep synced are written to disk.

Check your serialized items into source control, and use the Sync in the control panel to move updates from others into your database.
You can also automate doing a sync as part of a CI deployment process; see the readme on GitHub

Want deeper documentation? The README.md on GitHub is your friend: https://github.com/kamsar/Unicorn/blob/master/README.md

Have questions? Tweet @kamsar.

Found a bug? Send me a pull request on GitHub if you're feeling awesome: https://github.com/kamsar/Unicorn
(or an issue if you're feeling lazy)