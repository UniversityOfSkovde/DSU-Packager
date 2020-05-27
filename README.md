# DSU Packager
Unity Editor Script created to make the process of sharing unity assets easier.

Packages are a built-in feature of Unity. This plugin adds a custom editor window that makes it easier to create a new package. The plugin also configures the new package folder as a local git repository with all the recommended settings for Unity projects (.gitignore, .gitattributes, git LFS, etc.).

## Prerequisites
1. Make sure you have the following software installed:
* Git
* Git LFS
* Git Flow
2. Open Unity and open the Package Manager (located under `Window -> Package Manager`)
3. Press the `+` icon and select `Add Package From git URL...`
4. Enter the URL: `https://github.com/UniversityOfSkovde/DSU-Packager.git` and press `Add`
5. You can now find the plugin in the top menu under `DSU -> Create New Package`

## Publishing a Unity Package
Do you have some assets you want to share with the world using git?

1. In the top menu, select `DSU -> Create New Package`
2. Fill in the name, author and license you want for the new package
3. Press `Generate Package Folder!` and wait a few seconds

The new package should now shop up in the `Project` as a subdirectory of `Packages`
1. Move all the assets you want to share to the `Runtime`-directory of your new package
2. Use the commands `git add -A` and `git commit -m "Uploading files"` to version control the files you just moved

To share the new package, open a browser and go to github.com (create a new account of necessary).
1. On GitHub.com, press `New` to create a new repository
2. **Do not** select `Initialize this repository with a README` or select a license/.gitignore
3. Follow the instructions shown under the header `…or push an existing repository from the command line` from root folder of the generated package.
4. Others can now add your package to their projects using the `Add Package From git URL...` option described above.

## License
Copyright 2020 Emil Forslund

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
