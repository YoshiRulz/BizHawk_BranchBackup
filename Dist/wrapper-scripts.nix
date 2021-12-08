{ lib
# infrastructure
, commentUnless
, versionAtLeast
, writeShellScriptBin
, writeText
# rundeps
, mesa
, mono
, openal
# other parameters
, debugPInvokes
, hawkVersion
, initConfig # pretend this is JSON; the following env. vars will be substituted by the wrapper script (if surrounded by double-percent e.g. `%%BIZHAWK_DATA_HOME%%`): `BIZHAWK_DATA_HOME`
}:
let
	glHackLibs = builtins.mapAttrs (k: v: builtins.concatStringsSep " " v) (rec {
		Fedora_33 = [
			"libdrm_amdgpu.so.1" "libdrm_nouveau.so.2" "libdrm_radeon.so.1" "libedit.so.0" "libelf.so.1" "libffi.so.6" "libLLVM-11.so" "libtinfo.so.6" "libvulkan.so.1"
		];
		Manjaro_21_0_3 = [ # should match Arch and Manjaro releases from '20/'21
			"libdrm_amdgpu.so.1" "libdrm_nouveau.so.2" "libdrm_radeon.so.1" "libedit.so.0" "libelf.so.1" "libffi.so.7" "libGLdispatch.so.0" "libicudata.so.69" "libicuuc.so.69" "libLLVM-11.so" "liblzma.so.5" "libncursesw.so.6" "libsensors.so.5" "libstdc++.so.6" "libvulkan.so.1" "libxml2.so.2" "libz.so.1" "libzstd.so.1"
		];
		LinuxMint_20_2 = [ # should match Ubuntu 20.04 and similar distros
			"libbsd.so.0" "libedit.so.2" "libLLVM-12.so.1" "libtinfo.so.6"
		] ++ Manjaro_21_0_3; #TODO split
	});
	initConfigFile = writeText "config.json" (builtins.toJSON ({
		LastWrittenFrom = if builtins.length (builtins.splitVersion hawkVersion) < 3 then "${hawkVersion}.0" else hawkVersion;
		PathEntries = {
			Paths = [
				({ "System" = "Global_NULL"; Type = "Base"; Path = "%%BIZHAWK_DATA_HOME%%"; } // lib.optionalAttrs (!versionAtLeast "2.7.1" hawkVersion) { "Ordinal" = 1; })
			];
		};
	} // initConfig));
in rec {
	wrapperScript = writeShellScriptBin "emuhawk-wrapper" ''
		set -e

		if [ ! -e "$BIZHAWK_HOME/EmuHawk.exe" ]; then
			printf "no such file: %s\n" "$BIZHAWK_HOME/EmuHawk.exe"
			exit 1
		fi

		if [ "$XDG_DATA_HOME" ]; then
			BIZHAWK_DATA_HOME="$XDG_DATA_HOME"
		else
			BIZHAWK_DATA_HOME="$HOME/.local/share"
		fi
		BIZHAWK_DATA_HOME="$BIZHAWK_DATA_HOME/emuhawk-monort-${hawkVersion}"
		mkdir -p "$BIZHAWK_DATA_HOME"
		cd "$BIZHAWK_DATA_HOME"

		if [ ! -e config.json ]; then
			cat ${initConfigFile} >config.json # cp kept the perms as 444 -- don't @ me
			sed -i "s@%%BIZHAWK_DATA_HOME%%@$BIZHAWK_DATA_HOME@g" config.json
		fi

		export LD_LIBRARY_PATH=$BIZHAWK_HOME/dll:$BIZHAWK_GLHACKDIR:${lib.makeLibraryPath [ openal ]}
		${commentUnless debugPInvokes}export MONO_LOG_LEVEL=debug MONO_LOG_MASK=dll
		if [ "$1" = "--mono-no-redirect" ]; then
			shift
			printf "(received --mono-no-redirect, stdout was not captured)\n" >EmuHawkMono_laststdout.txt
			exec ${mono}/bin/mono $BIZHAWK_HOME/EmuHawk.exe --config=config.json "$@"
		else
			exec ${mono}/bin/mono $BIZHAWK_HOME/EmuHawk.exe --config=config.json "$@" >EmuHawkMono_laststdout.txt
		fi
	'';
	wrapperScriptNonNixOS = writeShellScriptBin "emuhawk-wrapper-non-nixos" ''
		set -e

		if [ "$XDG_STATE_HOME" ]; then
			BIZHAWK_GLHACKDIR="$XDG_STATE_HOME"
		else
			BIZHAWK_GLHACKDIR="$HOME/.local/state"
		fi
		export BIZHAWK_GLHACKDIR="$BIZHAWK_GLHACKDIR/emuhawk-monort-${hawkVersion}-non-nixos"

		thisScriptPath="${builtins.placeholder "out"}"
		if [ -e "$BIZHAWK_GLHACKDIR/populated_by_script" ]; then
			# symlinks have been set up already, check it was by this version of the script
			if [ "$(realpath "$BIZHAWK_GLHACKDIR/populated_by_script")" != "$thisScriptPath" ]; then
				rm "$BIZHAWK_GLHACKDIR/populated_by_script" # next step will remove the rest of the files
			fi
		fi
		if [ ! -e "$BIZHAWK_GLHACKDIR/populated_by_script" ]; then
			# either a broken link or it doesn't exist, start over
			printf "creating/recreating %s\n" "$BIZHAWK_GLHACKDIR"
			rm -fr "$BIZHAWK_GLHACKDIR"
			mkdir -p "$BIZHAWK_GLHACKDIR"
			ln -sT "$thisScriptPath" "$BIZHAWK_GLHACKDIR/populated_by_script" # does not register GC root!

			# symlink Nix-installed mesa for OpenTK, in such a way that it loads the libs and drivers we set up below and not Nix-installed ones (which won't work on non-NixOS)
			ln -svT "${lib.getOutput "drivers" mesa}/lib/libGLX_mesa.so.0" "$BIZHAWK_GLHACKDIR/libGLX_indirect.so.0"

			# symlink a bunch of libs (libGL and deps) from host in one dir, which can be added to LD_LIBRARY_PATH without it being polluted by other libs from host
			if [ "$(command -v lsb_release)" ]; then
				case "$(lsb_release -i | cut -c17- | tr -d "\n")" in
					"Arch"|"Artix"|"ManjaroLinux") libsToLink="${glHackLibs.Manjaro_21_0_3}";;
					"Fedora") libsToLink="${glHackLibs.Fedora_33}";;
					"Debian"|"LinuxMint"|"Pop"|"Ubuntu") libsToLink="${glHackLibs.LinuxMint_20_2}";;
				esac
			else
				printf "could not find lsb_release in PATH (install via host package manager, not Nix)\n"
			fi
			if [ -z "$libsToLink" ]; then
				printf "distro unknown or undetermined, assuming Arch/Manjaro\n"
				libsToLink="${glHackLibs.Manjaro_21_0_3}"
			fi
			for l in $libsToLink; do
				# could specify this in the distro case-esac too, but there's no benefit
				for d in /usr/lib64 /usr/lib /usr/lib/x86_64-linux-gnu /lib64 /lib; do
					if [ -e "$d/$l" ]; then
						ln -svT "$d/$l" "$BIZHAWK_GLHACKDIR/$l"
						break
					fi
				done
			done
		fi

		for d in /usr/lib64/dri /usr/lib/dri /usr/lib/x86_64-linux-gnu/dri; do
			if [ -e "$d" ]; then
				export LIBGL_DRIVERS_PATH=$d
				break
			fi
		done

		exec ${wrapperScript}/bin/emuhawk-wrapper "$@"
	'';
}
