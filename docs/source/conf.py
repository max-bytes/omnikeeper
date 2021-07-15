# Configuration file for the Sphinx documentation builder.
#
# This file only contains a selection of the most common options. For a full
# list see the documentation:
# https://www.sphinx-doc.org/en/master/usage/configuration.html

# -- Path setup --------------------------------------------------------------

# If extensions (or modules to document with autodoc) are in another directory,
# add these directories to sys.path here. If the directory is relative to the
# documentation root, use os.path.abspath to make it absolute, like shown here.
#
import os
import sys
# sys.path.insert(0, os.path.abspath('.'))

import sphinx_bootstrap_theme
import recommonmark
from recommonmark.transform import AutoStructify
from pygments.lexers.sql import PostgresConsoleLexer,PlPgsqlLexer

# -- Latex customization ------------------------------------------------------

# with open(os.path.dirname(__file__) + '/mhx-cd-theme/latex_preamble.tex', 'r+') as f:
#     latex_preamble = f.read()

# latex_elements = {
#     'preamble': latex_preamble,
# }


# -- Project information -----------------------------------------------------

project = 'omnikeeper Documentation'
copyright = '2021, maximiliancsuk'
author = 'Maximilian Csuk'


# -- General configuration ---------------------------------------------------

# Add any Sphinx extension module names here, as strings. They can be
# extensions coming with Sphinx (named 'sphinx.ext.*') or your custom
# ones.
extensions = [
    'recommonmark',
    'sphinx_rtd_theme',
    'sphinxcontrib.plantuml',
    'sphinxcontrib.drawio',
]

# Add any paths that contain templates here, relative to this directory.
templates_path = ['_templates']

# List of patterns, relative to source directory, that match files and
# directories to ignore when looking for source files.
# This pattern also affects html_static_path and html_extra_path.
exclude_patterns = []

source_suffix = {
    '.rst': 'restructuredtext',
    '.txt': 'markdown',
    '.md': 'markdown',
}

# drawio options
drawio_no_sandbox = True


# -- Options for PDF output --------------------------------------------------

latex_logo = 'images/omnikeeper_logo_v1.0.png' # set the title page logo


# -- Options for HTML output -------------------------------------------------

# The theme to use for HTML and HTML Help pages.  See the documentation for
# a list of builtin themes.
#
# html_theme = 'alabaster'

html_theme = 'bootstrap'
html_theme_path = sphinx_bootstrap_theme.get_html_theme_path()

# html_theme = "mhx-cd-theme"     # MHX CD Theme
# html_theme_path = ['.']         # Search for themes in current dir

drawio_builder_export_format = {"html": "svg", "latex": "pdf"}

# Add any paths that contain custom static files (such as style sheets) here,
# relative to this directory. They are copied after the builtin static files,
# so a file named "default.css" will overwrite the builtin "default.css".
html_static_path = []

def setup(app):
    app.add_config_value('recommonmark_config', {
            }, True)
    app.add_transform(AutoStructify)
    app.add_lexer('PostgresConsoleLexer', PostgresConsoleLexer)
    app.add_lexer('PlPgsqlLexer', PlPgsqlLexer)