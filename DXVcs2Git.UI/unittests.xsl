<?xml version="1.0"?>
<xsl:stylesheet
    xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.0">

    <xsl:output method="html"/>

    <xsl:variable name="nunit2.result.list" select="//test-results"/>
    <xsl:variable name="nunit2.suite.list" select="$nunit2.result.list//test-suite"/>
    <xsl:variable name="nunit2.case.list" select="$nunit2.suite.list/results/test-case"/>
    <xsl:variable name="nunit2.case.count" select="count($nunit2.case.list)"/>
    <xsl:variable name="nunit2.time" select="sum($nunit2.result.list/test-suite[position()=1]/@time)"/>
    <xsl:variable name="nunit2.failure.list" select="$nunit2.suite.list//failure"/>
    <xsl:variable name="nunit2.failure.count" select="count($nunit2.failure.list)"/>
    <xsl:variable name="nunit2.notrun.list" select="$nunit2.case.list/reason"/>
    <xsl:variable name="nunit2.notrun.count" select="count($nunit2.notrun.list)"/>

    <xsl:variable name="total.time" select="$nunit2.time"/>
    <xsl:variable name="total.notrun.count" select="$nunit2.notrun.count"/>
    <xsl:variable name="total.run.count" select="$nunit2.case.count - $total.notrun.count"/>
    <xsl:variable name="total.failure.count" select="$nunit2.failure.count"/>

    <xsl:template match="/">
            <xsl:if test="$total.run.count > 0 or $total.notrun.count > 0">
        <table class="section-table" cellpadding="2" cellspacing="0" border="0" width="98%">

            <!-- Unit Tests -->
            <tr>
                <td class="sectionheader" colspan="2">
                   Tests run: <xsl:value-of select="$total.run.count"/>, Failures: <xsl:value-of select="$total.failure.count"/>, Not run: <xsl:value-of select="$total.notrun.count"/>, Time: <xsl:value-of select="$total.time"/> seconds
                </td>
            </tr>

            <xsl:choose>
                <xsl:when test="$total.run.count = 0">
                    <tr><td colspan="2" class="section-data">No Tests Run</td></tr>
                    <tr><td colspan="2" class="section-error">This project doesn't have any tests</td></tr>
                </xsl:when>

                <xsl:when test="$total.failure.count = 0">
                    <tr><td colspan="2" class="section-data">All Tests Passed</td></tr>
                </xsl:when>
            </xsl:choose>

            <xsl:apply-templates select="$nunit2.failure.list"/>
            <xsl:apply-templates select="$nunit2.notrun.list"/>

            <tr><td colspan="2"> </td></tr>

            <xsl:if test="$total.failure.count > 0">
                <tr>
                    <td class="sectionheader" colspan="2">
                        Unit Test Failure and Error Details (<xsl:value-of select="$total.failure.count"/>)
                    </td>
                </tr>

                <xsl:call-template name="nunit2testdetail">
                    <xsl:with-param name="detailnodes" select="//test-suite[.//failure]"/>
                </xsl:call-template>
                <xsl:call-template name="nunit2testdetail">
                    <xsl:with-param name="detailnodes" select="//test-suite/results/test-case[.//failure]"/>
                </xsl:call-template>

                <tr><td colspan="2"> </td></tr>
            </xsl:if>
            
            <!--xsl:if test="$nunit2.notrun.count > 0">
                <tr>
                    <td class="sectionheader" colspan="2">
                        Warning Details (<xsl:value-of select="$nunit2.notrun.count"/>)
                    </td>
                </tr>
                <xsl:call-template name="nunit2testdetail">
                    <xsl:with-param name="detailnodes" select="//test-suite/results/test-case[.//reason]"/>
                </xsl:call-template>
                <tr><td colspan="2"> </td></tr>
            </xsl:if-->
        </table>
            </xsl:if>
            <xsl:if test="contains(., 'Unhandled exceptions:')">
        <table class="section-table" cellpadding="2" cellspacing="0" border="0" width="98%">

            <tr>
                <td class="sectionheader" colspan="2">Unhandled exceptions:</td>
            </tr>
            <tr>
            <td colspan="2" class="section-error">
                <pre>
                <xsl:value-of select="substring-before(substring-after(substring-after(.,'Unhandled exceptions:'), '&#xA;'), '&#xA;&#xA;&#xA;')"/>
                </pre>
            </td>
            </tr>
        </table>
            </xsl:if>
    </xsl:template>

    <!-- Unit Test Errors -->
    <xsl:template match="error">
        <tr>
            <xsl:if test="position() mod 2 = 0">
                <xsl:attribute name="class">section-oddrow</xsl:attribute>
            </xsl:if>
            <td class="section-data">Error</td>
            <td class="section-data"><xsl:value-of select="../@name"/></td>
        </tr>
    </xsl:template>

    <!-- Unit Test Failures -->
    <xsl:template match="failure">
        <tr>
            <xsl:if test="($nunit2.failure.count + position()) mod 2 = 0">
                <xsl:attribute name="class">section-oddrow</xsl:attribute>
            </xsl:if>
            <td class="section-data">Failure</td>
            <td class="section-data"><xsl:value-of select="../@name"/></td>
        </tr>
    </xsl:template>

    <!-- Unit Test Warnings -->
    <xsl:template match="reason">
        <!--tr>
            <xsl:if test="($total.failure.count + position()) mod 2 = 0">
                <xsl:attribute name="class">section-oddrow</xsl:attribute>
            </xsl:if>
            <td class="section-data">Warning</td>
            <td class="section-data"><xsl:value-of select="../@name"/></td>
        </tr-->
    </xsl:template>

    <!-- NUnit Test Failures And Warnings Detail Template -->
    <xsl:template name="nunit2testdetail">
        <xsl:param name="detailnodes"/>

        <xsl:for-each select="$detailnodes">
        
            <xsl:if test="failure">
            <tr><td colspan="2"><hr width="100%" color="#888888"/></td></tr>

            <tr><td class="section-data">Test:</td><td class="section-data"><xsl:value-of select="@name"/></td></tr>
            <tr><td class="section-data">Type:</td><td class="section-data">Failure</td></tr>
            <tr><td class="section-data">Message:</td><td class="section-data">
              <xsl:call-template name="br-replace">
                        <xsl:with-param name="word" select="failure//message"/>
              </xsl:call-template></td></tr>
            <tr>
                <td></td>
                <td class="section-error">
                    <pre><xsl:value-of select="failure//stack-trace"/></pre>
                </td>
            </tr>
            </xsl:if>

            <xsl:if test="reason">
            <tr><td class="section-data">Test:</td><td class="section-data"><xsl:value-of select="@name"/></td></tr>
            <tr><td class="section-data">Type:</td><td class="section-data">Warning</td></tr>
            <tr><td class="section-data">Message:</td><td class="section-data"><xsl:call-template name="br-replace">
                        <xsl:with-param name="word" select="reason//message"/>
                    </xsl:call-template></td></tr>
            </xsl:if>


        </xsl:for-each>
    </xsl:template>

    <xsl:template name="br-replace">
        <xsl:param name="word"/>
      <xsl:variable name="crD">
        <xsl:text>&#xD;</xsl:text>
      </xsl:variable>
      <xsl:variable name="crA">
        <xsl:text>&#xA;</xsl:text>
      </xsl:variable>
      <xsl:variable name="crAD"><xsl:text>
</xsl:text></xsl:variable>
      <xsl:choose>
        <xsl:when test="contains($word,$crAD)">
          <xsl:value-of select="substring-before($word,$crAD)"/>
          <br/>
          <xsl:call-template name="br-replace">
            <xsl:with-param name="word" select="substring-after($word,$crAD)"/>
          </xsl:call-template>
        </xsl:when>
        <xsl:when test="contains($word,$crA)">
          <xsl:value-of select="substring-before($word,$crA)"/>
          <br/>
          <xsl:call-template name="br-replace">
            <xsl:with-param name="word" select="substring-after($word,$crA)"/>
          </xsl:call-template>
        </xsl:when>
        <xsl:when test="contains($word,$crD)">
          <xsl:value-of select="substring-before($word,$crD)"/>
          <br/>
          <xsl:call-template name="br-replace">
            <xsl:with-param name="word" select="substring-after($word,$crD)"/>
          </xsl:call-template>
        </xsl:when>
        <xsl:otherwise>
                <xsl:value-of select="$word"/>
            </xsl:otherwise>
        </xsl:choose>
    </xsl:template>

</xsl:stylesheet>
