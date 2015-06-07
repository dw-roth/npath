npath
=====

npath is a tool I created for myself to ease viewing and updating the Windows PATH env variable. 

What it does
----

- Displays PATH entries one entry per line
- Displays PATH entries for the current user PATH, the system PATH, or both
- Permanently deletes a PATH entry from current user PATH or system PATH
- Permanently prepends or appends a PATH entry for the current user PATH or system PATH

Usage
----

To show user + system PATH environment variable

    npath [-v]
    
To show current user PATH environment variable

    npath -c user
    
To show system PATH environment variable

    npath -c system
    
To delete an entry from the current user PATH environment variable

    npath -c user -d "path\to\be\deleted"
    
To delete an entry from the system PATH environment variable:

    npath -c system -d "path\to\be\deleted"
    
To prepend an entry to the current user PATH environment variable:

    npath -c user -p "path\to\be\prepended
    
To append an entry to the system PATH environment variable:

    npath -c system -a "path\to\be\appended"
    
Only tested on Windows 7 Ultimate with SP 1. Works on my machine :) Before using this tool or any tool that manipulates the Windows registry, it would be a good idea to open regedit and export your registry in case something goes awry.

Setting it up
----

Copy the files in the dist directory to a folder on your Windows computer (I've only tested on Windows 7 Ultimate with SP1)

OR

Clone, build, and run

Created with Microsoft Visual Studio Ultimate 2012 







    

