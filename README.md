# paster
This program base64 encodes a file, then pastes chuncks it into an activated window using 'Ctrl+V'. Ideal for transferring files to text-restricted inputs, it divides the content into manageable chunks. Users should zip files first for efficiency.

The chunk size and paste interval can be adjusted with '-c' and '-sb' options to suit different needs. Best to use with `stty -echo` because this often is a big bottleneck.

# Usecase
When you want to copy files to a Kubernets pod that has no internet access but you have a terminal on that pod.

*Example* -c 60 = 60KB chuncks -sb = 0.5 seconds. Larger files take some time but I don't know a better way:

![img1](https://github.com/BuFuuu/paster/assets/6349896/363ee266-f6a6-4909-bced-d36a89db26d4)
![img2](https://github.com/BuFuuu/paster/assets/6349896/482eb465-e5f9-4029-b1ec-dd19e09a3782)

# Options:

- `--list-windows, -l`  
  List all open windows titles.

- `--sleep-after-first-window, -s`  
  Sleep time after first window activation. Default is 1.0 seconds.

- `--sleep-between-chunks, -sb`  
  Sleep time between chunks. Default is 1.0 seconds (1s).

- `--chunk-size, -c`  
  Size of each chunk in KB. Default is 800 (0.8MB).

- `--file-path, -f`  
  Path to the file to be processed.

- `--target-window-title, -t`  
  Title of the target window.

- `--manual-activation, -m`  
  Do not activate the window. Just start pasting. User needs to activate the window.

- `--shift-paste, -sp`  
  Use Ctrl+Shift+V to paste instead of Ctrl+V.

- `--echo-wrap, -ew`  
  Wrap in an `echo -n "base64 content" >> filename` statement and send RETURN after pasting.
