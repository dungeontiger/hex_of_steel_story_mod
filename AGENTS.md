The SPEC file includes the instructions and the requirements for the project.
Follow the instructions there and implement the requirements.
As new requirements or changes to requirements are needed the SPEC file will be updated.
When you are creating the code and the project, follow the best practises for that type of project.
For example, there are different best practices for creating a Python project, a Java project, or a .NET project.
All changes need to be tracked in github.
This is my user profile in Git Hub.  Use this.  https://github.com/dungeontiger
If a project is new, create a github repo for it. The name will be the same name as the project folder.
Use short but appropriate commit messages.
Push changes to github after major changes. You don't need to push everytime a change is made.
For each code change you make, you need to implement appropriate, meaningful unit tests.
For each higher level features, you need to have appropriate higher level tests
There should be some level of end to end tests.
After you make changes, run all tests and make sure they pass. 
Do not make superficial changes to make the tests pass. Only change the test if the test really needs to change.
Clearly document everything you do in the STATUS file.
Ensure that there is a debug and release mode to what you build.
The debug mode will generate verbose logging and other information which will help you solve problems.
Release mode will only log info when something crashes or unexpected happens.
If the application or mod crashes, be sure to capture as much context as possible so that you can fix the issue.
Before committing any changes perform a code review.
Address any issues you find in the code review and continue the process