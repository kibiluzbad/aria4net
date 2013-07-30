#--------------------------------------
# Dependencies
#--------------------------------------
require 'albacore'
#--------------------------------------
# Environment vars
#--------------------------------------
@env_solutionname = 'Aria4net'
@env_solutionfolderpath = "src/"

@env_buildfolderpath = 'build'
@env_version = "1.0.1"
@env_buildversion = @env_version + (ENV['env_buildnumber'].to_s.empty? ? "" : ".#{ENV['env_buildnumber'].to_s}")
@env_buildconfigname = ENV['env_buildconfigname'].to_s.empty? ? "Release" : ENV['env_buildconfigname'].to_s
@env_buildname = "#{@env_solutionname}-v#{@env_buildversion}-#{@env_buildconfigname}"

#--------------------------------------
# Albacore config
#--------------------------------------
Albacore.configure do |config|
    config.log_level = :verbose
    #config.msbuild.use :net4
end

#--------------------------------------
# Albacore flow controlling tasks
#--------------------------------------
desc 'Run default'
task :default => [:buildIt, :publish]
#--------------------------------------
task :testIt => [:unittests]

#--------------------------------------
# Albacore tasks
#--------------------------------------
assemblyinfo :versionIt do |asm|
	sharedAssemblyInfoPath = "#{@env_solutionfolderpath}/SharedAssemblyInfo.cs"
	asm.input_file = sharedAssemblyInfoPath
	asm.output_file = sharedAssemblyInfoPath
	asm.version = @env_version
	asm.file_version = @env_buildversion  
end

task :ensureCleanBuildFolder do
	FileUtils.rm_rf(@env_buildfolderpath)
	FileUtils.mkdir_p(@env_buildfolderpath)
end

msbuild :buildIt => [:ensureCleanBuildFolder] do |msb|
	msb.properties :configuration => @env_buildconfigname
	msb.targets :Clean, :Build
	msb.solution = "#{@env_solutionfolderpath}/#{@env_solutionname}.sln"
end

nunit :unittests do |nunit|
	tests = FileList["src/**/#{@env_buildconfigname}/*.Tests.dll"].exclude(/obj\//)
	nunit.command = "#{@env_solutionfolderpath}packages/NUnit.2.5.10.11092/tools/nunit-console.exe"
	nunit.options "/framework=v4.0.30319","/xml=#{@env_buildfolderpath}/NUnit-Report-#{@env_solutionname}-UnitTests.xml"
	nunit.assemblies tests
end

task :publish do |msb|
    Dir.rmdir(@env_buildfolderpath)
	Dir.mkdir(@env_buildfolderpath)
    Dir.mkdir("#{@env_buildfolderpath}/lib")
	Dir.mkdir("#{@env_buildfolderpath}/lib/tools")
	Dir.mkdir("#{@env_buildfolderpath}/lib/tools/aria2-1.16.3-win-32bit-build1")	

    FileUtils.cp_r FileList["src/**/#{@env_buildconfigname}/*.dll", "src/**/*.ps1"].exclude(/obj\//).exclude(/.Tests/), "#{@env_buildfolderpath}/lib"
	FileUtils.cp_r FileList["tools/aria2-1.16.3-win-32bit-build1/*"], "#{@env_buildfolderpath}/lib/tools/aria2-1.16.3-win-32bit-build1"
end

desc "create the nuget package"
nugetpack :pack => [:buildIt, :publish] do |nuget|
   nuget.command     = "tools/nuget.exe"
   nuget.nuspec      = "nuget-package/Aria4net.nuspec"
   nuget.base_folder = "nuget-package/"
   nuget.output      = "nuget-package/"
   nuget.symbols     = true
end