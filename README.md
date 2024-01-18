# paster
This program base64 encodes a file, then pastes it into an activated window using 'Ctrl+V'. Ideal for transferring files to text-restricted inputs, it divides the content into manageable chunks. Users should zip files first for efficiency.

The chunk size and paste interval can be adjusted with '-c' and '-sb' options to suit different needs. Best to use with stty -echo because this often is a big bottleneck.

# Usecase
When you want to copy files to a Kubernets pod that has no internet access but you have a terminal on that pod.

