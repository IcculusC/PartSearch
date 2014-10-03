module.exports = (grunt) ->
    grunt.loadNpmTasks('grunt-contrib-compress')

    grunt.initConfig
        pkg: grunt.file.readJSON("package.json")
        compress:
            main:
                options:
                    archive: 'release/PartSearch-<%= pkg.version %>.zip'
                
                files: [
                        { cwd: 'bin/Debug/', src: ['PartSearch.dll'], dest: 'PartSearch', expand: true }
                        { cwd: 'PartSearch/', src: ['**'], dest: 'PartSearch', expand: true }
                        { src: ['README.md', 'LICENSE.md'], dest: 'PartSearch'}
                    ]