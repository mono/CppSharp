  curl "https://raw.githubusercontent.com/andreafabrizi/Dropbox-Uploader/master/dropbox_uploader.sh" -o dropbox_uploader.sh

  echo APPKEY= > ~/.dropbox_uploader
  echo APPSECRET= >> ~/.dropbox_uploader
  echo ACCESS_LEVEL=sandbox >> ~/.dropbox_uploader
  echo OAUTH_ACCESS_TOKEN= >> ~/.dropbox_uploader
  echo OAUTH_ACCESS_TOKEN_SECRET= >> ~/.dropbox_uploader

  ./dropbox_uploader.sh upload llvm_linux_x86_64.7z llvm_linux_x86_64.7z