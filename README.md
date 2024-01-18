# paster
This program base64 encodes a file, then pastes it into an activated window using 'Ctrl+V'. Ideal for transferring files to text-restricted inputs, it divides the content into manageable chunks. Users should zip files first for efficiency.

The chunk size and paste interval can be adjusted with '-c' and '-sb' options to suit different needs. Best to use with stty -echo because this often is a big bottleneck.

# Usecase
When you want to copy files to a Kubernets pod that has no internet access but you have a terminal on that pod.
![img1](https://github.com/BuFuuu/paster/assets/6349896/363ee266-f6a6-4909-bced-d36a89db26d4)
![img2](https://github.com/BuFuuu/paster/assets/6349896/482eb465-e5f9-4029-b1ec-dd19e09a3782)
