# git-bin

Dealing with large binary files in git can be difficult. git-bin's goal is to allow files to be managed with git, but to store the file contents in Amazon S3. git-bin follows the principle of least surprise, and functions just like built-in git commands.

To make better use of time, bandwidth, and storage space, git-bin divides large files into smaller chunks (default size 1M). If there is a 1 byte change in the middle of a 100MB file, only one chunk will have to be uploaded and stored instead of the entire file.


## Installation

Extract the contents of git-bin-*.zip into a directory that is in your PATH. 


## Configuration

Step 1: Add a new filter called 'bin'

```bash
$ git config --global filter.bin.clean "git bin clean %f"
$ git config --global filter.bin.smudge "git bin smudge"
```

Step 2: Supply your Amazon S3 information

```bash
$ git config --global git-bin.s3bucket "your bucket name"
$ git config --global git-bin.s3key "your key"
$ git config --global git-bin.s3secretKey "your secret key"
```

Step 3: Set up .gitattributes

Create or edit the .gitattributes file that is in the root of the git repo that you want to use git-bin with. Specify file extensions that should be run through git-bin, for example:

```
*.jpg filter=bin binary
*.png filter=bin binary
*.bmp filter=bin binary
*.psd filter=bin binary
etc, etc, etc
```


## Usage

Once the three configuration steps have been completed, normal use of git will invoke git-bin for files that match a pattern in .gitattributes.

Adding a new file (note that the JPG is 217k, but only 409b were sent to git):

```bash
$ ls -lh brooklyn_bridge.jpg
217k Dec 20 15:03 brooklyn_bridge.jpg
 
$ git add brooklyn_bridge.jpg
[git-bin] Cleaning brooklyn_bridge.jpg

$ git commit -m "Added JPG that's manage by git-bin"
[dev 6adb9c6] Added JPG that's manage by git-bin
<snip>
 
$ git bin push
[git-bin] Uploading 1 chunks...
  [0/1] -> 0..10..20..30..40..50..60..70..80..90..100

$ git push
<snip>
Writing objects: 100% (3/3), 409 bytes, done.
```

When you or someone else on the team checks out a file it will either be pulled from the on-disk cache or downloaded if it's not in the cache:

```bash
$ git pull
<snip>
Updating dc53749..6adb9c6
[git-bin] Smudging brooklyn_bridge.jpg... Downloading 1 chunks...
  [0/1] -> 0..10..20..30..40..50..60..70..80..90..100
Fast-forward
 brooklyn_bridge.jpg |  Bin 0 -> 222616 bytes
 1 files changed, 0 insertions(+), 0 deletions(-)
 create mode 100644 brooklyn_bridge.jpg
```


## How it works

When a file is passed to the clean filter it is divided into chunks (see Optional Configuration section below to find out how to change the chunk size). The chunks are saved into a cache directory located at `<repo root>/.git/git-bin`. The output of the clean filter, and hence that data that git sees, is a YAML document. Here's what that JPG turned into:

```
Filename: brooklyn_bridge.jpg
ChunkHashes:
- 523A59D8C7460C9E43637A77FCA05989153E279E2E2F6A6FFF417671FFE93073
```

When a file is checked out the YAML gets passed to the smudge filter. The smudge filter looks for each chunk in the cache directory and downloads it if it's missing. The output of the smudge filter, and hence the data that gets written to the working directory, is the reassembled contents of the chunks.


## Optional configuration

The chunk size defaults to 1M. If you want to change this for some reason you can set it like any other value in your git config:

```bash
$ git config --global git-bin.chunkSize 10m
```

git has a setting called `core.bigFileThreshold` that it uses to try to deal with large files. The default value is 512M. Any file that's larger than this setting **will NOT get passed to git-bin!** You can of course set it to a larger value:

```bash
$ git config --global core.bigFileThreshold 2g
```

You can configure where the git-bin cache is located. By default, it is the .git/git-bin folder
within your repo. If you put it outside the repo (say, in ~/git-bin-cache) then you won't
re-download all the chunks if you have to blow away your repo and re-clone. This is also useful
if you want to pre-populate a build machine image with the majority of the chunks.

```bash
$ git config --global git-bin.cacheDirectory ~/git-bin-cache
```
