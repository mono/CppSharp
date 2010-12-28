<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0" 
xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
exclude-result-prefixes="xsl"

>
	<xsl:output method="xml" version="1.0" encoding="UTF-8" indent="no"/>
	<xsl:strip-space elements="*" />
	
	<xsl:template match="/">
		<root>
		<xsl:apply-templates />
		</root>
	</xsl:template>

	<xsl:template match="*[@mangled]">
		<pair><xsl:copy-of select="@mangled" /><xsl:copy-of select="@demangled" /></pair>
		<xsl:apply-templates />
	</xsl:template>

</xsl:stylesheet>
