# encoding: utf-8
require 'rubygems'
require 'albacore'
require 'rake/clean'
require 'rexml/document'

NANCY_VERSION = ""
OUTPUT = "build"
CONFIGURATION = 'Release'
SHARED_ASSEMBLY_INFO = 'dependencies/Nancy/src/SharedAssemblyInfo.cs'
SOLUTION_FILE = 'src/Nancy.Serialization.JsonNet.sln'

Albacore.configure do |config|
    config.log_level = :verbose
    config.msbuild.use :net4
	config.xunit.command = "dependencies/Nancy/tools/xunit/xunit.console.clr4.x86.exe"
end

desc "Compiles solution and runs unit tests"
task :default => [:clean, :version, :compile, :publish, :package]

#Add the folders that should be cleaned as part of the clean task
CLEAN.include(OUTPUT)
CLEAN.include(FileList["src/**/#{CONFIGURATION}"])

desc "Update shared assemblyinfo file for the build"
assemblyinfo :version => [:clean] do |asm|
    NANCY_VERSION = get_assembly_version SHARED_ASSEMBLY_INFO

    asm.version = NANCY_VERSION
    asm.company_name = "Nancy"
    asm.product_name = "Nancy.Serialization.JsonNet"
    asm.title = "Nancy.Serialization.JsonNet"
    asm.description = "Provides JSON (de)serialization support using Newtonsoft.JsonNet."
    asm.copyright = "Copyright (C) Andreas Hakansson, Steven Robbins and contributors"
    asm.output_file = SHARED_ASSEMBLY_INFO
end

desc "Compile solution file"
msbuild :compile => [:version] do |msb|
    msb.properties :configuration => CONFIGURATION
    msb.targets :Clean, :Build
    msb.solution = SOLUTION_FILE
end

desc "Gathers output files and copies them to the output folder"
task :publish => [:compile] do
    Dir.mkdir(OUTPUT)
    Dir.mkdir("#{OUTPUT}/binaries")

    FileUtils.cp_r FileList["src/**/#{CONFIGURATION}/*.dll", "src/**/#{CONFIGURATION}/*.pdb"].exclude(/obj\//).exclude(/.Tests/), "#{OUTPUT}/binaries"
end

desc "Executes xUnit tests"
task :xunit => :compile do
  puts "No tests"
end

desc "Zips up the built binaries for easy distribution"
zip :package => [:publish] do |zip|
    Dir.mkdir("#{OUTPUT}/packages")

    zip.directories_to_zip "#{OUTPUT}/binaries"
    zip.output_file = "Nancy.Serialization.JsonNet-Latest.zip"
    zip.output_path = "#{OUTPUT}/packages"
end

desc "Generates NuGet packages for each project that contains a nuspec"
task :nuget_package => [:publish] do
	Dir.mkdir("#{OUTPUT}/nuget")
    nuspecs = FileList["src/Nancy.Serialization.JsonNet/*.nuspec"]
    root = File.dirname(__FILE__)

    # Copy all project *.nuspec to nuget build folder before editing
    FileUtils.cp_r nuspecs, "#{OUTPUT}/nuget"
    nuspecs = FileList["#{OUTPUT}/nuget/Nancy.Serialization.JsonNet.nuspec"]

    # Update the copied *.nuspec files to correct version numbers and other common values
    nuspecs.each do |nuspec|
        update_xml nuspec do |xml|
            # Override the version number in the nuspec file with the one from this rake file (set above)
            xml.root.elements["metadata/version"].text = NANCY_VERSION

            # Override the Nancy dependencies to match this version
            nancy_dependencies = xml.root.elements["metadata/dependencies/dependency[contains(@id,'Nancy')]"]
            nancy_dependencies.attributes["version"] = "[#{NANCY_VERSION}]" unless nancy_dependencies.nil?

            # Override common values
            xml.root.elements["metadata/authors"].text = "Andreas Håkansson, Steven Robbins and contributors"
            xml.root.elements["metadata/summary"].text = "Nancy is a lightweight web framework for the .Net platform, inspired by Sinatra. Nancy aim at delivering a low ceremony approach to building light, fast web applications."
            xml.root.elements["metadata/licenseUrl"].text = "https://github.com/NancyFx/Nancy.Serialization.JsonNet/blob/master/license.txt"
            xml.root.elements["metadata/projectUrl"].text = "http://nancyfx.org"
        end
    end

    # Generate the NuGet packages from the newly edited nuspec fileiles
    nuspecs.each do |nuspec|        
        nuget = NuGetPack.new
        nuget.command = "dependencies/Nancy/tools/nuget/nuget.exe"
        nuget.nuspec = "\"" + root + '/' + nuspec + "\""
        nuget.output = "#{OUTPUT}/nuget"
        nuget.parameters = "-Symbols", "-BasePath \"#{root}\""     #using base_folder throws as there are two options that begin with b in nuget 1.4
        nuget.execute
    end
end

desc "Pushes the nuget packages in the nuget folder up to the nuget gallary and symbolsource.org. Also publishes the packages into the feeds."
task :nuget_publish, :api_key do |task, args|
    nupkgs = FileList["#{OUTPUT}/nuget/*#{NANCY_VERSION}.nupkg"]
    nupkgs.each do |nupkg| 
        puts "Pushing #{nupkg}"
        nuget_push = NuGetPush.new
        nuget_push.apikey = args.api_key if !args.empty?
        nuget_push.command = "dependencies/Nancy/tools/nuget/nuget.exe"
        nuget_push.package = "\"" + nupkg + "\""
        nuget_push.create_only = false
        nuget_push.execute
    end
end

def update_xml(xml_path)
    #Open up the xml file
    xml_file = File.new(xml_path)
    xml = REXML::Document.new xml_file
 
    #Allow caller to make the changes
    yield xml
 
    xml_file.close
         
    #Save the changes
    xml_file = File.open(xml_path, "w")
    formatter = REXML::Formatters::Default.new(5)
    formatter.write(xml, xml_file)
    xml_file.close 
end

def get_assembly_version(file)
  return '' if file.nil?

  File.open(file, 'r') do |file|
    file.each_line do |line|
      result = /\[assembly: AssemblyVersion\(\"(.*?)\"\)\]/.match(line)

      return result[1] if !result.nil?
    end
  end

  ''
end
