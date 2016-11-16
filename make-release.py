#!/usr/bin/env python3

import os
import re
import subprocess
import sys
import zipfile


def find_version():
    assembly_title_version, assembly_version, assembly_file_version = None, None, None
    for line in open('Sereema/Properties/AssemblyInfo.cs'):
        line = line.strip()
        matcher = re.match('\[assembly: AssemblyTitle\("Windfit Scada Uploader (\d+\.\d+\.\d+)"\)\]$', line)
        if matcher is not None:
            assembly_title_version = matcher.group(1)
        matcher = re.match('\[assembly: AssemblyVersion\("(\d+\.\d+\.\d+\.\d+)"\)\]$', line)
        if matcher is not None:
            assembly_version = matcher.group(1)
        matcher = re.match('\[assembly: AssemblyFileVersion\("(\d+\.\d+\.\d+\.\d+)"\)\]$', line)
        if matcher is not None:
            assembly_file_version = matcher.group(1)
    if assembly_title_version is None:
        raise ReleaseException('Assemblyinfo.cs: AssemblyTitle not found')
    if assembly_version is None:
        raise ReleaseException('AssemblyInfo.cs: AssemblyVersion not found')
    if assembly_file_version is None:
        raise ReleaseException('AssemblyInfo.cs: AssemblyFileVersion not found')
    if assembly_version != assembly_file_version:
        raise ReleaseException('AssemblyInfo.cs: AssemblyVersion and AssemblyFileVersion do not match')
    if assembly_version.split('.')[:3] != assembly_title_version.split('.'):
        raise ReleaseException('AssemblyInfo.cs: AssemblyTitle version does not match AssemblyVersion prefix')
    return assembly_title_version


def make_release(version):
    if subprocess.call(['git', 'rev-parse', 'v{}'.format(version)], stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL) == 0:
        raise ReleaseException('The release number v{} is already tagged in git, please update AssemblyInfo.cs metadata before releasing.'.format(version))
    if subprocess.check_output(['git', 'status', '--porcelain']) != b'':
        raise ReleaseException('Your git working copy is not clean, please commit everything before releasing.')
    if not os.path.exists('Sereema/bin/Release/Windfit-Send.exe'):
        raise ReleaseException('The release binary file is missing, please build before releasing.')
    if os.path.getmtime('Sereema/bin/Release/Windfit-Send.exe') < os.path.getmtime('Sereema/Properties/AssemblyInfo.cs'):
        raise ReleaseException('The release binary is older than the AssemblyInfo.cs metadata file, please rebuild before releasing.')
    subprocess.check_call(['git', 'tag', '-a', '-m', 'Version {}'.format(version), 'v{}'.format(version)])
    print('Tagged release in git: v{}'.format(version))
    with zipfile.ZipFile('Windfit-Send-{}.zip'.format(version), 'w', compression=zipfile.ZIP_DEFLATED) as zip_file:
        zip_file.write('Sereema/bin/Release/Windfit-Send.exe', 'Windfit-Send.exe')
    print('Created release archive: Windfit-Send-{}.zip'.format(version))


class ReleaseException(Exception):
    pass

if __name__ == '__main__':
    try:
        make_release(find_version())
    except ReleaseException as e:
        print('Error:', e, file=sys.stderr)
        sys.exit(-1)
