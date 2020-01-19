node {
    ansiColor('xterm') {
        applyJenkinsOptions()
        checkoutContent()
        loadUp('jenkins-unity-config.json')

        catchError {
            executeSetUpStage()
            executeUnityExportStageIfNeeded()
            executeBuildStageIfNeeded()
            executeDistributionStageIfNeeded()
        }
        
        postBuildFailureMessagesIfNeeded()
    }
}


SetUpStage setUpStage = null
UnityExportStage unityExportStage = null;
BuildStage buildStage = null
DistributionStage distributionStage = null

void applyJenkinsOptions() {
    properties([
            buildDiscarder(logRotator(numToKeepStr: '15')),
            disableConcurrentBuilds()])
}

void checkoutContent() {
    checkout scm
}

void loadUp(String filename) {
    Map config = readJSON file: filename
    Map environment = config.environment
    Map stages = config.stages
    Map setUp = stages.setUp
    Map unityExport = stages.unityExport
    Map build = stages.build
    Map distribution = stages.distribution

    setUpStage = getSetUpStage(setUp)
    unityExportStage = getUnityExportStage(environment, unityExport)
    buildStage = getBuildStage(environment, build)
    distributionStage = getDistributionStage(distribution, buildStage)
}

void executeSetUpStage() {
    stage(setUpStage.title) {
        run(setUpStage.dependenciesInstallationCommand())
        executeCocoapodsStepIfNeeded()
        executeCarthageStepIfNeeded()
    }
}

void executeCocoapodsStepIfNeeded() {
    CocoaPodsStep cocoaPodsStep = setUpStage.cocoaPodsStep
    if (cocoaPodsStep.isEnabled) {
        run(cocoaPodsStep.executionCommand())
    }
}

void executeUnityExportStageIfNeeded() {
    if (unityExportStage.isEnabled) {
        stage(unityExportStage.title) {
            String[] executionCommandList = unityExportStage.executionCommands()
            executionCommandList.each { executionCommand ->
                run(executionCommand)
            }
        }
    }
}

void executeBuildStageIfNeeded() {
    if (buildStage.isEnabled) {
        stage(buildStage.title) {
            String[] executionCommandList = buildStage.executionCommands()
            executionCommandList.each { executionCommand ->
                run(executionCommand)
            }
        }
    }
}

void executeDistributionStageIfNeeded() {
    if (distributionStage.isEnabled) {
        stage(distributionStage.title) {
            executeFirebaseDistributionStepIfNeeded()
        }
    }
}

void executeFirebaseDistributionStepIfNeeded() {
    FirebaseDistributionStep firebaseDistributionStep = distributionStage.firebaseDistributionStep
    if (firebaseDistributionStep.isEnabled) {
        withCredentials([file(credentialsId: 'GOOGLE_APPLICATION_CREDENTIALS', variable: 'GOOGLE_APPLICATION_CREDENTIALS')]) {
            run(firebaseDistributionStep.executionCommand())
        }
    }
}

// --------------------
// --- Set Up Stage ---
// --------------------
class SetUpStage extends Stage {
    CocoaPodsStep cocoaPodsStep

    SetUpStage(
            String title,
            CocoaPodsStep cocoaPodsStep) {
        super(true, title)
        this.cocoaPodsStep = cocoaPodsStep
    }
    
    String dependenciesInstallationCommand() {
        return "bundle install"
    }
}

class CocoaPodsStep implements StageStep {
    Boolean isEnabled
    String podFile
    
    CocoaPodsStep(Boolean isEnabled, String podFile) {
        this.isEnabled = isEnabled
        this.podFile = podFile
    }
    
    String executionCommand() {
        return "bundle exec fastlane pods" +
                ParamBuilder.getPodFileParam(podFile)
    }
}

SetUpStage getSetUpStage(Map setUp) {
    Map cocoaPods = setUp.cocoaPods
    CocoaPodsStep cocoaPodsStep = new CocoaPodsStep(
            cocoaPods.isEnabled,
            cocoaPods.podFile)

    return new SetUpStage(setUp.title, cocoaPodsStep)
}


// --------------------------
// --- Unity Export Stage ---
// --------------------------
class UnityExportStage extends Stage {
    String unityVersion
    String[] platformList

    UnityExportStage(
            Boolean isEnabled, 
            String title, 
            String unityVersion, 
            String[] platforms) {
        super(isEnabled, title)
        this.unityVersion = unityVersion
        this.platformList = platforms
    }

    String[] executionCommand() {
        String[] executionCommandList = []
        platformList.each { platform ->
            String executionMethod = executionMethod(platform)
            if (executionMethod) {
                String executionCommand = "/Applications/Unity/Hub/Editor/${unityVersion}/Unity.app/Contents/MacOS/Unity" +
                        " -quite" +
                        " -batchmode" +
                        " -executeMethod ${executionMethod}" +
                        " -nographics"
                executionCommandList += executionCommand
            }
        }
        return executionCommandList
    }

    private String executionMethod(String platform) {
        switch (platform) {
            case "iOS": return "Jenkins.PerformIOSBuild"
            case "android": return "Jenkins.PerformAndroidBuild"
            default:
                println("ERROR! Unsupported platform: " + platform)
                return null
        }
    }
}

UnityExportStage getUnityExportStage(Map environment, Map unityExport) {
    String[] platformList = getStringListFromJSONArray(unityExport.platforms)
    UnityExportStage unityExportStage = new UnityExportStage(
            platformList.size() > 0,
            unityExport.title,
            environment.unityVersion,
            platformList)

    return unityExportStage
}

// -------------------
// --- Build Stage ---
// -------------------
class BuildStage extends Stage {
    String projectFilename
    String workspaceFilename
    String outputPath
    BuildItem[] itemList
    
    BuildStage(
            Boolean isEnabled,
            String title,
            String projectFilename,
            String workspaceFilename,
            String outputPath,
            BuildItem[] items) {
        
        super(isEnabled, title)
        this.projectFilename = projectFilename
        this.workspaceFilename = workspaceFilename
        this.outputPath = outputPath
        this.itemList = items
    }
    
    String[] executionCommands() {
        String[] executionCommandList = []
        itemList.each { item ->
            String executionCommand = "bundle exec fastlane build" +
                    ParamBuilder.getProjectFilenameOrWorkspaceFilenameParam(projectFilename, workspaceFilename) +
                    ParamBuilder.getConfigurationParam(item.configuration) +
                    ParamBuilder.getSchemeParam(item.scheme) +
                    ParamBuilder.getOutputPathParam(outputPath) +
                    ParamBuilder.getOutputNameParam(item.name) +
                    ParamBuilder.getExportMethodParam(item.exportMethod) +
                    ParamBuilder.getProvisioningProfilesParam(item.profilesValue())
            executionCommandList += executionCommand
        }
        return executionCommandList
    }
    
    String buildPath(String buildId) {
        String path = ""
        itemList.each { item ->
            if (buildId.equals(item.id)) {
                path = outputPath + "/" + item.name
            }
        }
        return path
    }
}

class BuildItem {
    String id
    String name
    String configuration
    String scheme
    String exportMethod
    BuildProfile[] profileList
    
    BuildItem(
            String id,
            String name,
            String configuration,
            String scheme,
            String exportMethod,
            BuildProfile[] profiles) {
    
        this.id = id
        this.name = name
        this.configuration = configuration
        this.scheme = scheme
        this.exportMethod = exportMethod
        this.profileList = profiles
    }
    
    String profilesValue() {
        String[] valueList = []
        profileList.each { profile ->
            valueList += profile.value()
        }
        String value = valueList.join(',')
        return value
    }
}

class BuildProfile {
    String id
    String name
    
    BuildProfile(String id, String name) {
        this.id = id
        this.name = name
    }
    
    String value() {
        return id + "=>" + name
    }
}

BuildStage getBuildStage(Map environment, Map build) {
    List itemList = build.items
    
    BuildItem[] buildItemList = []
    itemList.each { item ->
        List profileList = item.provisioningProfiles
        BuildProfile[] buildProfileList = []
        profileList.each { profile ->
            BuildProfile buildProfile = new BuildProfile(
                profile.id,
                profile.name
            )
            buildProfileList += buildProfile
        }
        
        BuildItem buildItem = new BuildItem(
                item.id,
                NameBuilder.getOutputName(environment.projectName, item.id),
                item.configuration,
                item.scheme,
                item.exportMethod,
                buildProfileList
        )
        buildItemList += buildItem
    }

    return new BuildStage(
            buildItemList.size() > 0,
            build.title,
            NameBuilder.getProjectFilename(environment.projectName),
            NameBuilder.getWorkspaceFilename(environment.workspaceName),
            environment.outputPath + "/gym",
            buildItemList)
}


// --------------------------
// --- Distribution Stage ---
// --------------------------
class DistributionStage extends Stage {
    StageStep firebaseDistributionStep
    
    DistributionStage(
            Boolean isEnabled,
            String title,
            FirebaseDistributionStep firebaseDistributionStep) {
            
        super(isEnabled, title)
        this.firebaseDistributionStep = firebaseDistributionStep
    }
}

class FirebaseDistributionStep implements StageStep {
    Boolean isEnabled
    String firebaseAppId
    String[] testersGroupIdList
    String buildPath

    FirebaseDistributionStep(
            String firebaseAppId,
            String[] testersGroupIds,
            String buildPath) {

        this.isEnabled = firebaseAppId?.trim()
        this.firebaseAppId = firebaseAppId
        this.testersGroupIdList = testersGroupIds
        this.buildPath = buildPath
    }

    String executionCommand() {
        return "firebase" +
                " appdistribution:distribute \"${buildPath}\"" +
                " --groups \"${testersGroupIdList.join(',')}\"" +
                " --release-notes \"\"" +
                " --app ${firebaseAppId}"
    }
}

DistributionStage getDistributionStage(Map distribution, BuildStage buildStage) {
    Map firebaseDistribution = distribution.firebase
    FirebaseDistributionStep firebaseDistributionStep = new FirebaseDistributionStep(
            firebaseDistribution.appId,
            getStringListFromJSONArray(firebaseDistribution.testersGroupIds),
            buildStage.buildPath(firebaseDistribution.buildId))

    return new DistributionStage(
            firebaseDistributionStep.isEnabled,
            distribution.title,
            firebaseDistributionStep)
}


// ---------------
// --- Helpers ---
// ---------------
interface StageStep {
    Boolean isEnabled
    String executionCommand()
}

class Stage {
    Boolean isEnabled
    String title
    
    Stage(Boolean isEnabled, String title) {
        this.isEnabled = isEnabled
        this.title = title
    }
}

class NameBuilder {
    static String getProjectFilename(projectName) {
        return projectName + ".xcodeproj"
    }

    static String getWorkspaceFilename(workspaceName) {
        return (workspaceName.getClass() == String) ? (workspaceName + ".xcworkspace") : null
    }
    
    static String getOutputName(projectName, buildId) {
        return projectName + "-" + buildId + ".ipa"
    }
}

class ParamBuilder {
    static String getProjectFilenameParam(String projectFilename) {
        return " projectFilename:" + projectFilename
    }

    static String getWorkspaceFilenameParam(String workspaceFilename) {
        return (workspaceFilename == null) ? "" : (" workspaceFilename:" + workspaceFilename)
    }
    
    static String getProjectFilenameOrWorkspaceFilenameParam(String projectFilename, String workspaceFilename) {
        return (workspaceFilename == null) ?
            getProjectFilenameParam(projectFilename) :
            getWorkspaceFilenameParam(workspaceFilename)
    }

    static String getOutputPathParam(String outputPath) {
        return " outputPath:" + outputPath
    }

    static String getConfigurationParam(String configuration) {
        return " configuration:" + configuration
    }

    static String getExportMethodParam(String exportMethod) {
        return " exportMethod:" + exportMethod
    }

    static String getProvisioningProfilesParam(String provisioningProfiles) {
        return " provisioningProfiles:" + "\"" + provisioningProfiles + "\""
    }

    static String getOutputNameParam(String outputName) {
        return " outputName:" + "\"" + outputName + "\""
    }

    static String getSchemeParam(String scheme) {
        return " scheme:" + "\"" + scheme + "\""
    }

    static String getPodFileParam(String podFile) {
        return " podFile:" + "\"" + podFile + "\""
    }
}

String[] getStringListFromJSONArray(array) {
    String[] stringList = []
    array.each { value ->
        stringList += value
    }
    return stringList
}

void makeDirectory(String path) {
    run("mkdir -p ${path}")
}

void run(String command) {
    sh command
}







void postBuildFailureMessagesIfNeeded() {
    if (isJobResultFlagSuccessful()) {
        return
    }
    
    postSlackFailureMessage(getDefaultSlackChannelName())
    postEmailFailureMessage()
}

void postSlackFailureMessage(String channel) {
    String slackUrl = "https://telesoftas.slack.com/services/hooks/jenkins-ci/"
    String buildName = getBuildName()
    String author = getAuthor()
    String buildUrl = getBuildUrl()
    String message = "Build Failed: *${buildName}*" +
            "\nAuthor: *${author}*" +
            "\nCause: `Failure`" +
            "\nUrl: ${buildUrl}"
    String colorHex = "FF0000"
    
    slackSend message: message,
            notifyCommitters: true,
            tokenCredentialId: 'pirates-crew-slack-bot-token',
            channel: "",
            color: colorHex
}

void postEmailFailureMessage() {
    String authorEmail = getAuthorEmail()
    step([$class: 'Mailer',
            notifyEveryUnstableBuild: true,
            recipients: authorEmail,
            sendToIndividuals: true])
}

Boolean isJobResultFlagSuccessful() {
    return currentBuild.currentResult == "SUCCESS"
}

String getBuildName() {
    String jobName = URLDecoder.decode(env.JOB_NAME, "UTF-8")
    String buildNumber = env.BUILD_DISPLAY_NAME
    return jobName + " " + buildNumber
}

String getBuildUrl() {
    return env.BUILD_URL
}

String getAuthor() {
    String author = sh(script: "git log -1 --format='%an <%ae>' HEAD", returnStdout: true).trim()
    return author
}

String getAuthorEmail() {
    String authorEmail = sh(script: "git log -1 --format='%ae' HEAD", returnStdout: true).trim()
    return authorEmail
}

String getDefaultSlackChannelName() {
    return "#jenkins"
}

String getAuthorSlackName() {
    String authorEmail = getAuthorEmail()
    String accessToken = 'pirates-crew-slack-token'
    String slackEndpoint = "https://slack.com/api/users.lookupByEmail?token=${accessToken}&email=${authorEmail}"
    String response = sh(script: "curl ${slackEndpoint}", returnStdout: true).trim()
    println(response)
    Map json = readJSON text: response
    println(json)
    if (json["ok"]) {
        String authorSlackName = json["user"]["name"]
        return authorSlackName
    }
    
    return null
}
