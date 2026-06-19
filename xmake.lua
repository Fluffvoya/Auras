set_project("Aura")
set_version("0.1.0")

set_languages("c++23")
set_warnings("all", "extra")

add_rules("plugin.compile_commands.autoupdate")

if is_mode("debug") then
    set_symbols("debug")
    set_optimize("none")
end

if is_mode("release") then
    set_symbols("hidden")
    set_optimize("fastest")
    set_strip("all")
end

includes("modules/vocal")