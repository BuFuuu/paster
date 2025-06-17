# paster
This tool base64 encodes a file and pastes it into another application in chunks. Zip files first for efficiency. The target window will be focused automatically unless `-m` is used.

Chunk size and delays are configurable with `-C` and `-D`. Use `stty -echo` for best results on slow terminals.

# Usecase
E.g., when you want to copy files to a Kubernetes pod that has no internet access but you have a terminal on that pod.

**Example** `-C 60` = 60KB chunks every `-D 0.5` seconds.

![img1](https://github.com/BuFuuu/paster/assets/6349896/363ee266-f6a6-4909-bced-d36a89db26d4)
![img2](https://github.com/BuFuuu/paster/assets/6349896/482eb465-e5f9-4029-b1ec-dd19e09a3782)

# Options

- `-L, --list`
  List open window titles and show decode examples.
- `-A, --after <sec>`  
  Delay after window activation. Default 1 second.
- `-D, --delay <sec>`  
  Delay between chunks. Default 1 second.
- `-C, --chunk <kb>`  
  Chunk size in KB. Default 800.
- `-p, --path <file>`  
  File to transfer.
- `-w, --window <title>`  
  Title of the target window.
- `-m, --manual`  
  Do not activate the window automatically.
- `-S, --shift`  
  Use Ctrl+Shift+V instead of Ctrl+V.
- `-E, --echo <file>`  
  Wrap each chunk in an `echo` command and send ENTER.
