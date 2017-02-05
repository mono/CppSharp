osis()
{
    local n=0
    if [[ "$1" = "-n" ]]; then n=1;shift; fi

    # echo $OS|grep $1 -i >/dev/null
    uname -s |grep -i "$1" >/dev/null

    return $(( $n ^ $? ))
}

osis Darwin &&
{
	wget -O mono.pkg http://download.mono-project.com/archive/4.2.1/macos-10-x86/MonoFramework-MDK-4.2.1.102.macos10.xamarin.x86.pkg
	sudo installer -pkg mono.pkg -target /	
}

osis Linux &&
{
	sudo apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF
	echo "deb http://download.mono-project.com/repo/debian wheezy main" | sudo tee /etc/apt/sources.list.d/mono-xamarin.list
	sudo apt-get update -qq
	sudo apt-get install -y mono-devel
}
