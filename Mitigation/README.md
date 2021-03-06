The important directory is mounted on a seperate partition. Any time the script is run it will backup the contents of important into important2 and then unmount important.
This Prevents ransomware from being able to access the backed up files while maintaining a good level of availability for the backed up files. This script is relatively simple
which allows it to be ran automatically through systemmd, cron or others. The folder names can be changed as well as the users in the script to provided access to some users while not 
othes via CHMOD and CHOWN. This makes it harder for attackers to encrypt the data in the first place as they do not have the proper permissions to do so. Even if this is somehow circumvented
the backup partition can be used to restore any lost data. 
